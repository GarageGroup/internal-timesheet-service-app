# Telegram AI Agent: авторизация, App Registration и Managed Identity

Последнее обновление: 30.09.2026.

## Самая короткая версия

В схеме участвуют три разные личности:

1. **Человек** входит через Mini App под корпоративной учётной записью Entra ID.
2. **Telegram-бот** входит в Timesheet API под своей новой system-assigned Managed Identity.
3. **Timesheet API** обращается к Foundry и Storage под существующей user-assigned Managed Identity, а
   в Dataverse передаёт Entra Object ID человека как `CallerObjectId`.

Новая App Registration `api-timesheet-agent-test` представляет не человека и не самого бота. Она
представляет защищаемый **agent API**: задаёт audience токена и роль, которую должен иметь бот.

```text
Человек один раз входит в Mini App
        ↓
Telegram ID связывается с CRM/Entra-пользователем

Затем при каждом сообщении:

Telegram sender
        ↓
бот получает токен своей Managed Identity
        ↓
токен предназначен для api-timesheet-agent-test
        ↓
APIM предъявляет backend-сертификат
        ↓
Timesheet API проверяет токен, роль и allowlist
        ↓
API по BotId + TelegramUserId находит сохранённую привязку
        ↓
создаёт AgentUserContext
        ↓
Semantic Kernel получает инструменты, уже привязанные к этому пользователю
```

## Какие Azure-объекты добавлены

### App Registration `api-timesheet-agent-test`

Test-параметры:

| Параметр | Значение |
|---|---|
| Application/client ID | `3923995c-b131-4197-98c6-036072e68871` |
| Audience | `api://3923995c-b131-4197-98c6-036072e68871` |
| Application role | `Timesheet.Agent.Invoke` |
| Role ID | `8b52c4fa-b3ca-4293-99c8-06983d49fe19` |

Её смысл можно сформулировать так:

> «Я являюсь Timesheet Agent API. Токены для вызова моих внутренних endpoint должны быть выпущены для
> моего audience, а вызывающее приложение должно иметь роль `Timesheet.Agent.Invoke`».

App Registration не хранит связь Telegram-пользователя с CRM и не выполняет запросы. Она описывает
границу доступа к API.

### System-assigned Managed Identity Telegram-бота

Она включена у test Function App `func-internal-gtimesheet-test`:

| Параметр | Значение |
|---|---|
| Client ID | `ea0ce2ed-16af-4a3b-b891-6702e26841ac` |
| Principal ID | `7a8c4961-a12e-444d-9ca1-b11505241876` |

Этой identity назначена application role `Timesheet.Agent.Invoke` из App Registration. Поэтому бот
может получить app-only токен для нового agent API без client secret.

Важно различать идентификаторы:

- `client ID` попадает в claim `appid` или `azp` токена и используется API в allowlist;
- `principal ID` идентифицирует service principal при назначении Azure/Entra-ролей;
- ни один из них не является Telegram user ID или ID сотрудника.

### User-assigned Managed Identity Timesheet API

Это отдельная, уже существовавшая identity API. Она используется для исходящих обращений API к
Foundry и Azure Table по Azure RBAC. Настройка `AZURE_CLIENT_ID` выбирает её в Azure-hosting.

Она не заменяет identity бота:

```text
MI бота: кто вызывает Timesheet Agent API
MI API:  от чьего имени API вызывает Foundry и Storage
Пользователь: от чьего имени выполняется бизнес-операция в CRM
```

Если бы бот и API использовали одну identity, API не мог бы надёжно отличить внешний вызов бота от
собственного workload.

## Шаг 1. Как Mini App создаёт привязку человека

Mini App получает корпоративную авторизацию Entra ID через существующий пользовательский контур API.
В `UserSignInIn.SystemUserId` endpoint framework передаёт идентификатор авторизованного CRM-пользователя,
а в JSON приходит подписанный Telegram `initData`:

- [`UserSignInIn.cs`](../src/endpoint/User.SignIn/Contract/UserSignInIn.cs)
- [`Func.Invoke.cs`](../src/endpoint/User.SignIn/Endpoint/Func/Func.Invoke.cs)

Перед сохранением API проверяет подпись Telegram, срок `auth_date` и извлекает числовой Telegram user ID:

- [`TelegramWebAppDataValidator.cs`](../src/endpoint/User.SignIn/Endpoint/Telegram.Validation/TelegramWebAppDataValidator.cs)

Ключевой смысл проверки:

```csharp
var expectedHash = ComputeHash(option.BotToken, dataCheckString);
CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
```

После этого `UserSignInFunc` одновременно:

1. загружает корпоративного `systemuser`;
2. получает фактический Bot ID через Telegram Bot API;
3. проверяет, не связан ли Telegram-пользователь с другим сотрудником;
4. создаёт или обновляет серверную запись связи.

Таким образом, результат входа — не cookie для агента, а долговременная серверная привязка:

```text
BotId + TelegramUserId → CRM SystemUser + Entra Object ID
```

## Шаг 2. Как бот получает токен для нового App Registration

При запуске бота HTTP-клиенты agent API регистрируются с явно выбранной system-assigned identity:

- [`Host.Create.cs`](../../internal-timesheet-bot-app/src/AzureFunc/ApplicationHost/Host.Create.cs)

```csharp
new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
```

Перед каждым запросом `AgentAccessTokenHandler` читает `AgentApi:Audience`, добавляет `/.default`,
получает токен и записывает его в Bearer header:

- [`AgentAccessTokenHandler.cs`](../../internal-timesheet-bot-app/src/AzureFunc/AgentApi/AgentAccessTokenHandler.cs)

```csharp
var scope = audience.TrimEnd('/') + "/.default";
var accessToken = await credential.GetTokenAsync(new([scope]), cancellationToken);
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
```

В test:

```text
AgentApi__Audience=api://3923995c-b131-4197-98c6-036072e68871
```

Entra выдаёт токен только потому, что service principal Managed Identity бота имеет application role
`Timesheet.Agent.Invoke` на service principal нашего App Registration. Client secret не используется.

## Шаг 3. Зачем запрос проходит через APIM

Бот отправляет запрос на отдельный agent route:

```text
https://apim-integration-platform-test-01.azure-api.net/timesheet-agent/
```

APIM:

- не удаляет Bearer-токен Managed Identity;
- предъявляет Timesheet backend клиентский сертификат;
- направляет запрос в `app-garage-timesheet-service-test`.

Сертификат доказывает, что запрос пришёл через разрешённый сетевой шлюз. JWT доказывает, что
инициатором является разрешённая Managed Identity бота. Это две разные проверки.

## Шаг 4. Как API проверяет App Registration и Managed Identity

JWT Bearer handler настраивается здесь:

- [`Host.Configure.cs`](../src/app/Application/Host/Host.Configure.cs)

API проверяет:

- подпись токена;
- tenant и issuer;
- audience App Registration;
- срок жизни токена.

После криптографической проверки middleware выполняет прикладную авторизацию:

- [`AgentAccessMiddleware.cs`](../src/app/Application/Authentication/AgentAccessMiddleware.cs)

Порядок проверок:

1. Agent feature flag включён.
2. JWT успешно проверен схемой `TimesheetAgent`.
3. В claim `roles` есть `Timesheet.Agent.Invoke`.
4. `azp` или `appid` совпадает с client ID разрешённой MI.
5. Для этого client ID в конфигурации задан конкретный Telegram `BotId`.

```csharp
var clientId = principal.FindFirstValue("azp") ?? principal.FindFirstValue("appid");
```

`BotId` не берётся из тела запроса. Middleware добавляет его как доверенный claim:

```csharp
identity.AddClaim(new("timesheet_bot_id", botId.ToString(...)));
```

Это важно: даже разрешённый клиент не может написать в JSON идентификатор другого Telegram-бота.

Конфигурация имеет следующий смысл:

```text
Agent__Authentication__Audience
    → какое App Registration защищает API

Agent__Authentication__RequiredRole
    → какая application role обязательна

Agent__Authentication__Clients__0__ClientId
    → какая Managed Identity может обращаться

Agent__Authentication__Clients__0__BotId
    → какому Telegram-боту соответствует эта identity
```

## Шаг 5. Как API сопоставляет сообщение с человеком

Входной контракт agent endpoint берёт `BotId` только из созданного middleware claim, а Telegram user/chat
ID — из фактического update, переданного ботом:

- [`AgentMessageSendIn.cs`](../src/endpoint/Agent.Message.Send/Contract/AgentMessageSendIn.cs)
- [`Func.Invoke.cs`](../src/endpoint/Agent.Message.Send/Endpoint/Func/Func.Invoke.cs)

```csharp
new AgentUserIdentity(input.BotId, input.TelegramUserId, input.TelegramChatId)
```

Затем `AgentUserContextResolver` ищет активную привязку строго по паре:

```text
BotId + TelegramUserId
```

- [`AgentUserContextResolver.cs`](../src/service/Agent.Identity/Core/Resolver/AgentUserContextResolver.cs)
- [`Binding.Filter.cs`](../src/service/Agent.Identity/Core/Internal.DbAgentUserBinding/Binding.Filter.cs)
- [`Binding.Field.cs`](../src/service/Agent.Identity/Core/Internal.DbAgentUserBinding/Binding.Field.cs)

Resolver также требует приватный чат (`TelegramChatId == TelegramUserId`), единственную активную связь,
неотключённого CRM-пользователя и заполненный Entra Object ID. Результат:

- [`AgentUserContext.cs`](../src/service/Agent.Identity/Contract/AgentUserContext.cs)

```text
AgentUserContext
├─ BotId
├─ TelegramUserId
├─ TelegramChatId
├─ BindingId
├─ CrmSystemUserId
└─ EntraObjectId
```

Если связи нет, агент не запускается: endpoint возвращает `UserNotLinked`, а бот предлагает войти через
Mini App. Если связь помечена signed-out или CRM-пользователь отключён, запрос также отклоняется.

## Шаг 6. Почему AI не может выбрать другого пользователя

`AgentUserContext` создаётся до вызова Semantic Kernel. Инструменты Kernel замыкают этот контекст внутри
своих экземпляров. В параметрах функций модели отсутствуют `BotId`, `SystemUserId`, `EntraObjectId` и
`CallerObjectId`.

Например, чтение списаний всегда получает доверенный Entra ID из контекста:

- [`Timesheet.GetSet/Func.Invoke.cs`](../src/service/Agent/Core/Timesheet.GetSet/Func.Invoke.cs)

Создание, изменение и удаление при Confirm делают то же самое:

- [`Timesheet.ConfirmCreate/Func.Invoke.cs`](../src/service/Agent/Core/Timesheet.ConfirmCreate/Func.Invoke.cs)
- [`Timesheet.ConfirmUpdate/Func.Invoke.cs`](../src/service/Agent/Core/Timesheet.ConfirmUpdate/Func.Invoke.cs)
- [`Timesheet.ConfirmDelete/Func.Invoke.cs`](../src/service/Agent/Core/Timesheet.ConfirmDelete/Func.Invoke.cs)

Во всех случаях источником является:

```csharp
context.EntraObjectId
```

Далее существующие Dataverse builders записывают его в `CallerObjectId`, например:

- [`TimesheetJson.cs`](../src/endpoint/Timesheet.Modify/Endpoint/Internal.Json/TimesheetJson.cs)
- [`TimesheetJson.cs`](../src/endpoint/Timesheet.Delete/Endpoint/Internal.Json/TimesheetJson.cs)

Сама модель получает только бизнес-параметры: дату, проект, длительность, комментарий или ID записи,
который API повторно проверяет среди доступных этому пользователю данных.

## Шаг 7. Почему подтверждение кнопкой тоже безопасно

Подготовленное действие сохраняет владельца и точный payload в `TimesheetAgentAction`. При нажатии
кнопки бот снова:

1. получает токен своей Managed Identity;
2. вызывает тот же защищённый agent API;
3. передаёт только `ActionId`, решение и фактические Telegram identifiers.

API снова строит `AgentUserContext` и action store сравнивает владельца. Параметры списания из callback
не принимаются. Поэтому знание или пересылка чужого `ActionId` не даёт права выполнить действие.

## Кто чему доверяет

| Участник | Что он доказывает | Чего он не доказывает |
|---|---|---|
| Mini App + Entra login | Корпоративную личность человека | Что последующий HTTP-вызов пришёл от бота |
| Telegram `initData` | Какой Telegram user открыл Mini App | Корпоративную личность без Entra login |
| MI Telegram-бота | Что agent API вызывает разрешённый workload | Какой именно человек написал сообщение |
| App Registration | Audience и требуемую application role agent API | Связь Telegram ↔ CRM |
| APIM certificate | Что backend-вызов прошёл через разрешённый APIM | Личность workload или человека |
| Server binding | Какой CRM/Entra user соответствует BotId + TelegramUserId | Подлинность вызывающего приложения без JWT |
| `AgentUserContext` | Итоговую доверенную личность бизнес-операции | Не является токеном и не приходит от модели |

## Итог

Фраза «пользователь авторизован в Mini App» в нашей системе означает:

> Для пары `BotId + TelegramUserId` существует одна активная серверная привязка к действующему
> CRM-пользователю с Entra Object ID.

А запрос агента допускается только когда одновременно выполнены оба условия:

```text
запрос подписан разрешённой Managed Identity бота
                         И
для фактического Telegram sender существует активная привязка
```

Только после этого API создаёт `AgentUserContext` и разрешает Semantic Kernel использовать инструменты.
Semantic Kernel не участвует ни в аутентификации, ни в выборе пользователя.

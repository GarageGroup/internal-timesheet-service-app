# Telegram AI Agent: журнал прогресса

Последнее обновление: 26.09.2026.

Связанный документ: [план реализации](telegram-ai-agent-implementation-plan.md).

Изменения тестовой инфраструктуры фиксируются отдельно в [журнале изменений Azure](telegram-ai-agent-azure-change-log.md).

Разрешённые будущему агенту операции и выявленные ограничения собраны в [каталоге операций](telegram-ai-agent-operation-catalog.md).

## Текущее состояние

Проект находится на **этапе 1 — Identity и привязка пользователя без LLM**.

Базовый модуль разрешения пользователя и усиленная проверка `User.SignIn` реализованы и покрыты unit-тестами. Resolver пока не подключён к HTTP endpoint, Telegram-боту или Semantic Kernel. Следующая задача — зарегистрировать resolver в приложении и создать первый внутренний read-only endpoint с вызовом `Profile.Get`.

```text
[x] Этап 0. Baseline и первичный аудит
[~] Этап 1. Identity и binding без LLM
[ ] Этап 2. Общие business factories и проверка прав
[ ] Этап 3. Semantic Kernel и read-only инструменты
[ ] Этап 4. Подтверждения и операции записи
[ ] Этап 5. Пилот и эксплуатация
```

Обозначения: `[x]` — завершено, `[~]` — выполняется, `[ ]` — не начато.

## Что уже сделано

### 25.09.2026 — архитектурный план

- Изучены API, Telegram-бот, mini app, SupportBot и консольный прототип Semantic Kernel.
- Выбрана архитектура: Semantic Kernel располагается в API, бот служит транспортом, бизнес-обработчики вызываются внутри процесса без HTTP-запросов API к самому себе.
- Определена схема авторизации: бот обращается к API с app-only токеном, а конечный пользователь определяется по серверной Telegram/CRM-привязке.
- Зафиксированы правила подтверждения изменяющих операций, идемпотентности, хранения заданий и постепенного включения функций.
- Создан файл `docs/telegram-ai-agent-implementation-plan.md`.

### 25.09.2026 — baseline

- Проверена версия SDK: .NET SDK 10.0.401.
- Выполнена исходная сборка и запуск всех существующих тестов.
- До добавления нового модуля все существующие тесты проходили.
- Подтверждено, что рабочее дерево до начала реализации содержало только новый каталог `docs/` с архитектурным документом.
- В репозитории и проверенных родительских каталогах не обнаружен `AGENTS.md`.

### 25.09.2026 — модуль Agent.Identity

В solution добавлены три проекта:

- `src/service/Agent.Identity/Contract`;
- `src/service/Agent.Identity/Core`;
- `src/service/Agent.Identity/Test`.

Реализованы:

- `AgentUserIdentity` — доверенные входные идентификаторы бота, Telegram-пользователя и чата;
- `AgentUserContext` — разрешённый контекст с `BotId`, `TelegramUserId`, `TelegramChatId`, ID привязки, CRM System User ID и Entra Object ID;
- `IAgentUserContextResolver` — публичный контракт resolver;
- `AgentUserContextResolver` — реализация поиска и проверки привязки;
- `AgentUserContextResolverDependency` — регистрация resolver в принятом в проекте стиле `PrimeFuncPack.Dependency`;
- `DbAgentUserBinding` — SQL-проекция привязки Telegram-пользователя и CRM-пользователя.

Resolver выполняет следующие проверки:

1. Идентификаторы бота и Telegram-пользователя должны быть положительными, chat ID не должен быть нулевым.
2. В первой версии разрешены только private chat, где `TelegramChatId == TelegramUserId`.
3. Поиск выполняется по активной CRM-записи и паре `(BotId, TelegramUserId)`.
4. Запрашивается не более двух записей, чтобы обнаружить неоднозначную привязку.
5. Отдельно обрабатываются отсутствующая привязка, несколько привязок и выполненный sign-out.
6. Проверяется, что CRM-пользователь не заблокирован.
7. Проверяется наличие Entra Object ID.
8. Проверяется корректность обязательных ID записи привязки и CRM-пользователя.
9. Ошибки SQL переводятся в типизированную ошибку `Unknown` с сохранением исходной причины.

Для соединения с `systemuser` используется `LEFT JOIN`. Благодаря этому существующая запись привязки с отсутствующим или некорректным CRM-пользователем не маскируется под обычное состояние `UserNotLinked`.

Добавлено 14 unit-тестов, покрывающих:

- неправильные входные идентификаторы;
- групповой чат;
- сформированный SQL-запрос;
- отсутствие привязки;
- инфраструктурную ошибку SQL;
- неоднозначную привязку;
- sign-out;
- заблокированного CRM-пользователя;
- отсутствие Entra Object ID;
- пустые обязательные ID;
- успешное создание пользовательского контекста.

Проверки после реализации:

- `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно;
- всего прошло 370 тестов, включая 14 новых;
- `git diff --check` — ошибок форматирования diff не обнаружено.

### 25.09.2026 — усиление User.SignIn

- Проверка Telegram Mini App `initData` вынесена в отдельный `TelegramWebAppDataValidator` с внедряемым `TimeProvider`.
- Параметры разбираются независимо от порядка; пустые, повторяющиеся и некорректно percent-encoded параметры отклоняются.
- HMAC-SHA256 вычисляется по алгоритму Telegram, hash декодируется как 32 байта и сравнивается через `CryptographicOperations.FixedTimeEquals`.
- `user.id` читается JSON-десериализацией, а не регулярным выражением.
- `auth_date` проверяется относительно серверного UTC-времени. По умолчанию TTL равен 5 минутам, clock skew — 30 секундам.
- TTL и clock skew добавлены в `appsettings.json` как `TelegramBot:WebAppDataMaxAgeMinutes` и `TelegramBot:WebAppDataClockSkewSeconds`.
- Перед upsert выполняется поиск активных привязок по `(BotId, TelegramUserId)`.
- Повторный вход того же CRM-пользователя остаётся идемпотентным.
- Привязка Telegram-пользователя к другому CRM-пользователю и несколько активных совпадений возвращают HTTP-конфликт `TelegramUserAlreadyLinked`.
- Предварительная проверка конфликта уменьшает риск неправильной привязки, но не заменяет уникальное ограничение в Dataverse: два одновременно выполняющихся первых входа теоретически всё ещё могут пройти проверку до записи.
- Добавлены тесты известного подписанного Telegram-вектора, срока действия, future timestamp, порядка параметров, подписи, JSON, дубликатов, Dataverse-запроса, повторного входа и конфликтов.

Проверки после реализации:

- `dotnet test src/endpoint/User.SignIn/Test/Test.csproj --no-restore` — успешно, 46 тестов;
- `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, всего 387 тестов;
- интеграционный вызов Telegram и реальная CRM не проверялись.

### 25.09.2026 — аудит Azure и закрытый контур agent API

- Подтверждено, что `func-internal-gtimesheet-test` размещает Telegram-бота, а API работает в `app-garage-timesheet-service-test`.
- В общем APIM найден API `garage-timesheet-api` с публичным префиксом `timesheet` и backend на App Service API.
- В APIM не обнаружена политика `validate-jwt`, Easy Auth у App Service выключен. Текущий `UseJwtReader()` нельзя считать достаточным доказательством криптографической проверки токена.
- Для маршрутов `/internal/agent/*` добавлен отдельный JWT Bearer handler. Он проверяет подпись Entra, issuer, audience, lifetime и signing key.
- После проверки токена дополнительно требуются app role и явное серверное сопоставление `client id -> BotId`. `BotId` не должен поступать из тела запроса.
- Контур выключен настройкой `Agent:Enabled = false`. В этом состоянии любой `/internal/agent/*` возвращает 404.
- При включении без tenant, audience, required role или хотя бы одного client mapping приложение завершится при старте с ошибкой конфигурации.
- Из `Profile.Get` выделена общая фабрика `UseProfileGetFunc`, чтобы будущий агент и существующий endpoint использовали одну бизнес-реализацию.
- Выяснено, что API и бот сейчас используют одну user-assigned managed identity. Для agent API нужна отдельная идентичность бота, иначе их невозможно различить по `azp`/`appid`.

Проверки:

- `dotnet build Internal.Timesheet.Service.slnx --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Service.slnx --no-build --no-restore` — успешно, 387 тестов;
- локальный запрос `POST /internal/agent/profile` при выключенном feature flag — HTTP 404;
- реальные Entra-токены ещё не проверялись, так как отдельная идентичность и app role пока не созданы.

### 25.09.2026 — первый внутренний read-only endpoint

- Добавлен endpoint `POST /internal/agent/profile`.
- В JSON принимаются только `TelegramUserId` и `TelegramChatId`; `BotId` поступает из доверенного claim `timesheet_bot_id`, сформированного после app-only аутентификации.
- `AgentUserContextResolver` подключён к приложению и разрешает Telegram-пользователя в серверный контекст.
- Профиль запрашивается через общую реализацию `IProfileGetFunc` с разрешённым `EntraObjectId`; бизнес-логика `Profile.Get` не дублируется.
- Ошибки разделены на invalid identity, отсутствующую привязку, недоступную привязку, отсутствующий профиль и неизвестную инфраструктурную ошибку.
- Legacy `UseJwtReader()` теперь применяется только к старым маршрутам. Маршруты `/internal/agent/*` проходят исключительно через новый Entra JWT Bearer контур.
- Добавлено 12 unit-тестов для всех вариантов resolver, передачи доверенного Entra ID, ошибок профиля и успешного результата.

Проверки:

- `dotnet build Internal.Timesheet.Service.slnx --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Service.slnx --no-build --no-restore` — успешно, 399 тестов;
- зарегистрированный `POST /internal/agent/profile` при `Agent:Enabled = false` возвращает HTTP 404;
- вызов с настоящим app-only токеном будет проверен после создания Azure identity и app role.

### 25.09.2026 — подготовка Entra для agent API

- У Function App `func-internal-gtimesheet-test` включена system-assigned managed identity.
- Существующая user-assigned identity сохранена; тип identity теперь `SystemAssigned, UserAssigned`.
- Создана App Registration `api-timesheet-agent-test` и соответствующий service principal.
- Для API настроен identifier URI и application role `Timesheet.Agent.Invoke` с типом участника `Application`.
- Роль назначена system-assigned identity Telegram-бота.
- В `app-garage-timesheet-service-test` записаны tenant, audience, required role и сопоставление identity бота с Telegram `BotId`.
- `Agent__Enabled` оставлен равным `false`; существующая версия приложения не открывает agent endpoint.
- Allowlist клиентов переведён с dictionary-ключа, содержащего GUID, на массив объектов `ClientId`/`BotId`, потому что App Service отклонил имя настройки с GUID-сегментом.
- Секреты и Telegram token в журнал не записывались.

### 25.09.2026 — Managed Identity клиент в Telegram-боте

- В `internal-timesheet-bot-app` добавлен typed HTTP client для `POST /internal/agent/profile`.
- Access token запрашивается через `ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)`, поэтому общая user-assigned identity не может быть случайно выбрана для agent-вызова.
- Scope формируется как `<AgentApi:Audience>/.default`, токен передаётся в стандартном заголовке Bearer.
- Добавлена диагностическая команда `/profile`; она передаёт Telegram chat ID как user/chat ID и выводит только имя и язык найденного профиля.
- Ошибка `404` предлагает пользователю выполнить вход через Mini App, остальные ошибки логируются без токена и возвращают нейтральное сообщение.
- В Function App записаны `AgentApi__BaseAddress` и `AgentApi__Audience`; приложение осталось в состоянии `Running`.
- Добавлен тестовый проект с тремя тестами scope/Bearer, HTTP-контракта и ошибочного статуса.
- Коммит бота: `a376719 Add managed identity agent profile client`.

Проверки:

- `dotnet build Internal.Timesheet.Bot.sln --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Bot.sln --no-restore` — успешно, 3 теста;
- версии API и бота ещё не развёрнуты, `Agent__Enabled` остаётся выключенным.

## Принятые решения

| Решение | Причина |
|---|---|
| Сначала реализовать identity без LLM | Авторизацию и выбор пользователя нельзя доверять модели |
| Хранить CRM ID и Entra Object ID раздельно | В существующих контрактах используются оба вида ID, и они не взаимозаменяемы |
| Не передавать ID пользователя как аргументы SK-функций | Пользовательский контекст должен поступать только из доверенного серверного resolver |
| Пока поддерживать только private chat | В private chat Telegram user ID совпадает с chat ID; для групп нужен отдельный безопасный сценарий |
| Загружать максимум две привязки | Этого достаточно, чтобы отличить единственную запись от неоднозначной конфигурации |
| Не подключать resolver к HTTP до усиления sign-in | Нельзя основывать новый доверенный контур на недостаточно проверенном `initData` |
| Не добавлять Semantic Kernel на этапе 1 | Сначала должна быть проверена детерминированная цепочка идентичности |
| Ограничить срок `initData` пятью минутами и 30 секундами clock skew | Снижает окно повторного использования подписанных данных; значения доступны в конфигурации |
| Конфликтующую привязку не перезаписывать автоматически | Перепривязка меняет владельца агентского контекста и требует отдельного явного flow |
| Защищать `/internal/agent/*` отдельным JWT Bearer handler | Существующий контур чтения claims и текущая конфигурация APIM не дают достаточной гарантии проверки токена |
| Держать agent API выключенным до готовности Entra | Неполная конфигурация безопасности не должна приводить к доступному endpoint |
| Определять `BotId` по доверенному client id | Вызывающая сторона не должна выбирать бот через пользовательский payload |
| Повторять структуру ближайшего существующего модуля | Единая структура `Contract`/`Endpoint`/`Func`/`Test.*` упрощает сопровождение и review |
| Использовать `AsyncPipeline`/`Pipeline` для orchestration | Новый код должен следовать принятой функциональной композиции и типизированной обработке `Result` |
| Сохранить `Agent.Profile.Get` до отдельного указания | Работающий endpoint нужен для демонстрации защищённого вертикального среза руководству |

### 26.09.2026 — успешная сквозная проверка `/profile`

- Пользователь авторизовался в тестовой Mini App, после чего отправил `/profile` тестовому Telegram-боту.
- Бот успешно получил app-only токен через system-assigned Managed Identity и вызвал отдельный agent API через APIM.
- API проверил Entra-токен, application role и allowlist клиента, определил доверенный `BotId`, разрешил Telegram-привязку и вернул профиль соответствующего CRM-пользователя.
- Бот показал пользователю данные его аккаунта. Повторный интерактивный вход для вызова `/profile` не потребовался.
- Отдельно проверены отрицательные сценарии на публичном agent endpoint: запрос без Bearer-токена возвращает HTTP 401; запрос с некорректным Bearer-токеном также возвращает HTTP 401.
- Таким образом, подтверждён вертикальный срез `Telegram -> bot -> Managed Identity -> APIM -> API -> user binding -> CRM profile`.
- Изменения инфраструктуры при этой проверке не выполнялись.

Не проверены отдельными интеграционными тестами: чужой валидный client ID, валидный токен без нужной роли, непривязанный Telegram-пользователь, групповой чат и поведение после sign-out.

### 28.09.2026 — выравнивание нового кода со стилем проекта

- `Agent.Profile.Get` приведён к структуре существующих endpoint: реализация перенесена в `Endpoint/Func`, тесты — в `Test/Test.Func`, сценарии расположены в `Func.Invoke.cs` и `Test.Invoke.cs`.
- `Agent.Identity` приведён к аналогичной структуре `Core/Resolver` и `Test/Test.Resolver`.
- Ручная async-orchestration `AgentProfileGetFunc` заменена на `AsyncPipeline` с типизированным преобразованием ошибок resolver и `Profile.Get`.
- Валидация, SQL-вызов и преобразование результата `AgentUserContextResolver` объединены в `AsyncPipeline`.
- В проект endpoint добавлена прямая зависимость `EarlyFuncPack.Core.AsyncPipeline` той же версии, что используется существующими endpoint.
- Основная инструкция дополнена обязательными правилами использования `AsyncPipeline`/`Pipeline`, повторения структуры ближайшего модуля и сохранения диагностического `Agent.Profile.Get` до отдельного решения владельца проекта.
- `Agent.Profile.Get` не удалён и продолжает оставаться демонстрационным вертикальным срезом.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 399 тестов прошли.

### 28.09.2026 — инвентаризация операций API

- Проанализированы все существующие endpoint и их внутренние business-функции.
- Операции разделены на read candidates, write blocked, system only и diagnostic.
- Для первой read-only итерации предложены профиль, периоды, последние/поисковые проекты, списания и теги.
- Зафиксировано требование отдельных agent adapters: модель не должна получать `SystemUserId`, `CallerObjectId`, `BotId`, Telegram ID или Entra Object ID как аргументы.
- Выявлен блокирующий риск `Timesheet.Update`: update input не устанавливает `CallerObjectId`, поэтому операцию нельзя подключать до аудита impersonation и ownership.
- Для `Project.GetSet` требуется проверить видимость Incident/Opportunity/Lead; для `Tag.GetSet` — происхождение Project ID; для всех списков — серверные лимиты.
- Код приложения, Azure и deployment в этом инкременте не изменялись.
- Создан `docs/telegram-ai-agent-operation-catalog.md`.

### 28.09.2026 — первый read-only agent adapter

- Добавлен модуль `src/service/Agent` со стандартной структурой `Contract/Core/Test`.
- Реализован adapter получения списаний за период поверх существующей `ITimesheetSetGetFunc`.
- Вход adapter содержит только даты; Entra Object ID подставляется из доверенного `AgentUserContext` и недоступен модели.
- Добавлена серверная проверка порядка дат и максимального диапазона; значение по умолчанию — 31 день.
- Результат преобразуется в отдельный agent DTO, не связанный с HTTP binding metadata.
- `TimesheetSetGetDependency` теперь отдельно предоставляет общую `ITimesheetSetGetFunc`; существующий HTTP endpoint продолжает использовать ту же реализацию.
- Добавлены 5 тестов: неправильный порядок дат, слишком длинный период, доверенный Entra ID, преобразование ошибки и успешное преобразование результата.
- Вопрос по отсутствующему `CallerObjectId` в `Timesheet.Update` помечен как обязательный для уточнения у руководства перед write-этапом.
- Adapter пока не подключён к Application, HTTP или Semantic Kernel; Azure не изменялся.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 404 теста прошли.

### 28.09.2026 — read-only adapter поиска проектов

- Добавлен adapter `Project.SearchSet` в существующий модуль `src/service/Agent`.
- Модель сможет задавать только поисковый текст и `top`; `CallerObjectId` подставляется из `AgentUserContext.EntraObjectId`.
- Уточнено различие идентификаторов: существующее свойство `ProjectSetSearchIn.SystemUserId` получено из claim `oid` и фактически содержит Entra Object ID, а не CRM primary key.
- Введены серверные ограничения: непустой текст до 100 символов, `top` от 1 до 20, значение по умолчанию 10.
- Существующая `IProjectSetSearchFunc` выделена из dependency composition и совместно используется HTTP endpoint и agent adapter.
- Ошибка доступа преобразуется в отдельный `Forbidden`, остальные ошибки — в `Unknown`.
- Adapter покрыт тестами в принятой структуре `Test/Test.Project.SearchSet`.
- Настройки пока не привязаны к `appsettings.json`, потому что agent-модуль ещё не зарегистрирован в Application.
- Application, HTTP, Semantic Kernel и Azure не изменялись.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 413 тестов прошли.

## Что ещё не сделано на текущем этапе

- Resolver не зарегистрирован в основном приложении, потому что внутренний endpoint ещё не создан.
- Нет внутреннего endpoint для Telegram-бота.
- Серверная app-only проверка реализована, но Entra application/app role и отдельная managed identity бота ещё не настроены.
- Не введены `BindingVersion`, отзыв ожидающих действий и централизованный logout.
- Не проверены и не исправлены существующие дубли привязок в реальной CRM; уникальное ограничение на `(BotId, TelegramUserId)` пока не добавлено.
- Не изменялась схема Dataverse.
- Не выполнялись интеграционные тесты с реальной CRM.
- Semantic Kernel и Azure AI Foundry ещё не подключались к API.
- Telegram-бот и mini app на этом этапе не изменялись.

## Следующий инкремент

После отдельного подтверждения развернуть диагностический вертикальный срез в test:

1. Опубликовать текущую ветку API в `app-garage-timesheet-service-test`.
2. Проверить, что при выключенном флаге внутренний endpoint возвращает 404, а старые маршруты работают.
3. Опубликовать текущую ветку бота в `func-internal-gtimesheet-test`.
4. Включить `Agent__Enabled=true` и проверить 401 без токена и 403 с неподходящим приложением.
5. Выполнить `/profile` из Telegram для уже привязанного тестового пользователя.
6. При любой проблеме снова установить `Agent__Enabled=false`; существующий Mini App flow не отключать.

## Открытые вопросы

- Подтвердить с владельцем безопасности выбранные значения: `auth_date` 5 минут и clock skew 30 секунд.
- Должен ли один CRM-пользователь иметь возможность привязать несколько Telegram-аккаунтов к одному боту?
- Нужно ли разрешать осознанную перепривязку Telegram-пользователя к другой корпоративной учётной записи, и каким подтверждением её защищать?
- Есть ли в CRM существующие дубли по `(BotId, TelegramUserId)`?
- Определить сетевой маршрут agent-вызовов: через общий APIM либо напрямую в App Service с дополнительными сетевыми ограничениями.
- Локальный `launchSettings.json` содержит Telegram bot token, но файл исключён через `.gitignore` и Git его не отслеживает. Ротация из-за одного только локального хранения не требуется; секрет всё равно не следует выводить в логи или пересылать.

## Правила ведения журнала

После каждого завершённого инкремента необходимо:

1. Обновить дату и текущий этап.
2. Кратко перечислить изменённые компоненты и фактическое поведение.
3. Записать принятые архитектурные решения и выявленные ограничения.
4. Указать выполненные команды проверки и их результат.
5. Отдельно отметить то, что не было проверено в реальной инфраструктуре.
6. Сформулировать один ближайший инкремент, не смешивая несколько крупных этапов.
7. Не записывать в журнал секреты, токены, Telegram `initData`, connection strings и персональные данные.
8. Любое изменение Entra ID, Managed Identity, App Service, Function App, APIM, webhook или deployment-конфигурации параллельно фиксировать в журнале изменений Azure с инструкцией для production.

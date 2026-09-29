# Telegram AI Agent: журнал изменений Azure

Последнее обновление: 28.09.2026.

Этот документ фиксирует инфраструктурные изменения тестового контура Telegram AI Agent. Он предназначен для аудита и последующего воспроизведения конфигурации в production. Секреты, ключи, токены, connection strings и персональные данные здесь не публикуются.

Связанные документы:

- [план реализации](telegram-ai-agent-implementation-plan.md);
- [журнал прогресса](telegram-ai-agent-progress.md);
- [обзор решения для руководителя](telegram-ai-agent-management-overview.md);
- [production rollout runbook](telegram-ai-agent-production-rollout.md).

## Область изменений

| Среда | Подписка | Resource Group | Ресурс |
|---|---|---|---|
| Test | `73a6f94e-bfb4-4926-a9dd-09228d53a2a5` | `rg-garage-timesheet-test` | `func-internal-gtimesheet-test` |
| Test | `73a6f94e-bfb4-4926-a9dd-09228d53a2a5` | `rg-garage-timesheet-test` | `app-garage-timesheet-service-test` |
| Test | `106dd084-8190-453f-87c9-cd2cb714b1d6` | `rg-integration-platform-test` | `apim-integration-platform-test-01` |
| Test | Entra tenant `69879a40-68bb-4b30-941b-eb9692ddd9b4` | — | App Registration `api-timesheet-agent-test` |

Production-ресурсы в рамках перечисленных работ не изменялись.

## 25.09.2026 — Entra ID и Managed Identity

### Function App `func-internal-gtimesheet-test`

- Включена system-assigned managed identity.
- Существующая user-assigned managed identity сохранена.
- Итоговый тип identity: `SystemAssigned, UserAssigned`.
- Client ID system-assigned identity: `ea0ce2ed-16af-4a3b-b891-6702e26841ac`.
- Principal ID system-assigned identity: `7a8c4961-a12e-444d-9ca1-b11505241876`.

Причина: вызовы agent API должны выполняться от отдельной идентичности Telegram-бота. Общая user-assigned identity не позволяет надёжно отличить бот от API по `appid`/`azp`.

### App Registration `api-timesheet-agent-test`

- Созданы App Registration и соответствующий service principal.
- Application (client) ID: `3923995c-b131-4197-98c6-036072e68871`.
- Identifier URI / audience: `api://3923995c-b131-4197-98c6-036072e68871`.
- Создана application role:
  - display value: `Timesheet.Agent.Invoke`;
  - allowed member type: `Application`;
  - role ID: `8b52c4fa-b3ca-4293-99c8-06983d49fe19`.
- Роль назначена service principal system-assigned identity Function App.

Причина: API проверяет не только валидность app-only токена, но также выделенную роль вызывающего приложения.

### Что повторить для production

1. Создать отдельную production App Registration с production-наименованием.
2. Создать application role `Timesheet.Agent.Invoke`.
3. Включить отдельную Managed Identity production-бота либо выбрать уже существующую выделенную identity.
4. Назначить роль только service principal этой identity.
5. Не переносить test client ID, principal ID и audience в production.

## 25–26.09.2026 — настройки App Service API

В `app-garage-timesheet-service-test` добавлена конфигурация agent-контура:

| Настройка | Назначение |
|---|---|
| `Agent__Enabled` | Feature flag внутренних agent endpoint |
| `Agent__TenantId` | Entra tenant, которому доверяет JWT Bearer handler |
| `Agent__Audience` | Audience токена agent API |
| `Agent__RequiredRole` | Обязательная application role |
| `Agent__Clients__0__ClientId` | Разрешённый client ID Managed Identity бота |
| `Agent__Clients__0__BotId` | Серверное сопоставление вызывающего приложения с Telegram-ботом |

Сначала `Agent__Enabled` был оставлен `false`, затем включён после подготовки identity и endpoint.

Массив `Agent__Clients` выбран вместо dictionary с GUID в имени: App Service не принял имя настройки с GUID-сегментом.

API использует App Service client certificate в режиме `Required`. Поэтому прямой HTTP-вызов от Function App был отклонён до JWT-проверки. Для agent-вызовов добавлен отдельный маршрут через APIM, который предъявляет backend-сертификат.

### Изменение JWT issuer 26.09.2026

Managed Identity выдала access token с issuer Azure AD v1:

`https://sts.windows.net/69879a40-68bb-4b30-941b-eb9692ddd9b4/`

Первоначальная конфигурация API допускала только issuer v2. Код API изменён так, чтобы принимать оба tenant-specific issuer:

- `https://sts.windows.net/{tenantId}/`;
- `https://login.microsoftonline.com/{tenantId}/v2.0`.

Коммит: `7d5f390 Accept managed identity token issuer`.

API напрямую развёрнут в test App Service ZIP deployment. Развёрнутая версия `7d5f390`, deployment status `RuntimeSuccessful`, один экземпляр запущен успешно. Health check через APIM вернул HTTP 200; `BotApi`, `DataverseApi` и `SqlApi` — `Healthy`.

## 25–26.09.2026 — APIM agent API

В `apim-integration-platform-test-01` создан отдельный API:

| Параметр | Значение |
|---|---|
| API ID | `garage-timesheet-agent-api` |
| Public path | `timesheet-agent` |
| Backend | `https://app-garage-timesheet-service-test.azurewebsites.net/` |
| Operation | `POST /internal/agent/profile` |
| Subscription required | `false` |

Policy операции:

- направляет запрос в App Service API;
- использует APIM certificate entity `timesheet-certificate` для backend-аутентификации;
- не подменяет Bearer token бота: JWT продолжает проверяться самим API.

Причина отдельного API: существующий APIM API `garage-timesheet-api` проверяет client ID Mini App и `systemUserId`, поэтому не подходит для app-only вызовов Telegram-бота. Ослаблять его политику ради agent-контура не стали.

Bot `AgentApi__BaseAddress` изменён на:

`https://apim-integration-platform-test-01.azure-api.net/timesheet-agent/`

### Что повторить для production

1. Создать отдельный agent API/маршрут в production APIM.
2. Указать production backend и production certificate entity.
3. Перенести operation/policy декларативно через принятую IaC/CI/CD схему.
4. Проверить, что Bearer header доходит до backend неизменённым.
5. Проверить недоступность backend без требуемого client certificate.
6. Не отключать и не расширять Mini App policy для agent-вызовов.

## 25.09.2026 — настройки и развёртывание Telegram-бота

В `func-internal-gtimesheet-test` добавлены настройки:

| Настройка | Назначение |
|---|---|
| `AgentApi__BaseAddress` | Базовый URL отдельного agent API в APIM |
| `AgentApi__Audience` | Audience для получения Managed Identity access token |

Бот переведён на явное использование system-assigned Managed Identity. После изменения конфигурации Function App перезапускался, health status проверялся.

Диагностическая версия бота с командой `/profile` была развёрнута напрямую в test Function App, так как обычный Git CI/CD требовал недоступные пользователю секреты. Production не изменялся.

Также исправлена конфигурация Telegram webhook тестового бота: webhook направлен на актуальный APIM ingress. Секрет webhook и Function key в журнале не фиксируются.

## 25.09.2026 — Mini App test

Для тестовой сборки Mini App исправлены:

- OAuth scope: заменён устаревший scope на зарегистрированный scope test API;
- API base URL: заменён неразрешимый `api.test.garage-group.io` на `https://apim-integration-platform-test-01.azure-api.net`.

Изменения сделаны только в test/local-конфигурации. Production environment-файл не изменялся.

Test Mini App развёрнут напрямую и затем успешно проверен: авторизация, `/signIn`, `/getProjects` и `/getTimesheets` возвращали успешные ответы, обращения к Dataverse проходили.

## 26.09.2026 — итоговая интеграционная проверка

- Команда `/profile` успешно вернула тестовому пользователю его CRM-профиль.
- Подтверждена полная цепочка Managed Identity, Entra app role, APIM client certificate, JWT-проверки API и серверной Telegram/CRM-привязки.
- Запрос без токена и запрос с некорректным Bearer-токеном получили HTTP 401.
- Во время этой проверки конфигурация и ресурсы Azure не изменялись.

## 28.09.2026 — подготовка Table Storage и аудит Foundry

### Выполненные изменения

В существующем Storage Account `stinternalgtimesheettest` создана таблица
`TimesheetAgentConversation` через Azure CLI с Entra-аутентификацией. Таблица предназначена только
для ограниченной истории диалогов агента. Connection string и storage key приложению не выдавались.

Production-ресурсы, APIM, Function App и настройки `app-garage-timesheet-service-test` не изменялись.
App Service не перезапускался.

### Подтверждённая инфраструктура

- рабочий API размещён в `app-garage-timesheet-service-test`;
- API использует user-assigned Managed Identity `id-internal-gtimesheet-test` с principal ID
  `7ec77991-a88b-40b6-a6b2-47233c6bf9bf`;
- существующий Foundry resource: `ai-garage-timesheet-test`;
- существующий Foundry project: `ai-timesheet-test`;
- project endpoint:
  `https://ai-garage-timesheet-test.services.ai.azure.com/api/projects/ai-timesheet-test`;
- отдельный App Service `app-garage-timesheet-agent-test` не является хостом разрабатываемого API и
  не должен получать его storage/Foundry-разрешения.

### Не выполнено и причина

Назначение роли `Storage Table Data Contributor` identity API на Storage Account отклонено Azure:
у текущего пользователя нет разрешения `Microsoft.Authorization/roleAssignments/write`.

Первоначально запланированное имя роли `Azure AI User` отсутствует в данной подписке: актуальное имя
этой Foundry-роли — `Foundry User`. Для используемого приложением прямого OpenAI v1 inference
connector документация Microsoft требует роль `Cognitive Services User` на scope Foundry resource;
назначить её должен пользователь с правом управления RBAC.

Deployment `gpt-5.4` не создан. Azure вернул `InsufficientQuota`: лимит
`OpenAI.GlobalStandard.gpt-5.4` в `North Europe` равен нулю. Дополнительная проверка usage показала,
что ненулевая квота в регионе сейчас есть только для embedding-модели, но не для chat-моделей.
Неудачная попытка deployment не создала ресурс и не начала потребление модели.

### Требуемые ручные действия

1. Назначить principal `7ec77991-a88b-40b6-a6b2-47233c6bf9bf` роль
   `Storage Table Data Contributor` на `stinternalgtimesheettest`.
2. Назначить тому же principal роль `Cognitive Services User` на Foundry resource, который будет
   использоваться приложением.
3. Запросить chat-model quota для выбранной модели и deployment type в `North Europe` либо выбрать
   другой регион/существующий корпоративный Foundry resource с доступной квотой.
4. После появления квоты создать deployment и только затем добавить в API настройки
   `Agent__Foundry__ProjectEndpoint`, `Agent__Foundry__ModelId` и
   `Agent__Storage__TableServiceEndpoint`.

### Откат и production checklist

Текущий шаг откатывается удалением только таблицы `TimesheetAgentConversation`; пока она пуста и код
не развёрнут, иных зависимостей у неё нет. Удаление автоматически не выполнялось.

Для production необходимо отдельно создать таблицу, выдать две минимальные роли production identity,
подтвердить регион и квоту модели, создать отдельный deployment и записать production endpoint/model
через принятую CI/CD или IaC-схему. Test resource IDs и principal ID переносить нельзя.

## 28.09.2026 — пересоздание Foundry в West Europe и deployment модели

Пользователь пересоздал test Foundry resource и проект в регионе `West Europe`:

| Объект | Значение |
|---|---|
| Foundry resource | `ai-timesheet-test` |
| Foundry project | `ai-timesheet-test` |
| Project endpoint | `https://ai-timesheet-test.services.ai.azure.com/api/projects/ai-timesheet-test` |

Через Azure CLI успешно создан deployment:

| Параметр | Значение |
|---|---|
| Deployment | `gpt-5-mini` |
| Model version | `2025-08-07` |
| Deployment type / SKU | `GlobalStandard` |
| Capacity | `10` тысяч токенов в минуту |
| Provisioning state | `Succeeded` |

Deployment использует доступную subscription quota в `West Europe`. Это pay-per-token deployment,
а не зарезервированная provisioned capacity. Для `GlobalStandard` регион ресурса не гарантирует регион
обработки inference; перед production необходимо отдельно подтвердить требования к обработке CRM-текста.

Повторные попытки назначить identity API роли `Storage Table Data Contributor` и
`Cognitive Services User` завершились `AuthorizationFailed`: текущая учётная запись не имеет
`Microsoft.Authorization/roleAssignments/write`. Роли не назначены, настройки App Service не
изменялись, приложение не перезапускалось. Администратору передаётся отдельный идемпотентный скрипт
назначения ролей.

После передачи скрипта администратор успешно назначил Managed Identity
`id-internal-gtimesheet-test` (principal ID `7ec77991-a88b-40b6-a6b2-47233c6bf9bf`) две роли:

| Роль | Scope |
|---|---|
| `Storage Table Data Contributor` | Storage Account `stinternalgtimesheettest` |
| `Cognitive Services User` | Foundry resource `ai-timesheet-test` |

Назначения повторно проверены через Azure CLI. Роли выданы resource-level identity API, а не
Telegram-боту, проектной identity Foundry или вспомогательному App Service.

В `app-garage-timesheet-service-test` через Azure CLI записаны настройки:

| Настройка | Значение / назначение |
|---|---|
| `Agent__Foundry__ProjectEndpoint` | endpoint test-проекта `ai-timesheet-test` |
| `Agent__Foundry__ModelId` | deployment `gpt-5-mini` |
| `Agent__Foundry__TokenScope` | `https://ai.azure.com/.default` |
| `Agent__Storage__TableServiceEndpoint` | Table endpoint `stinternalgtimesheettest` |
| `Agent__Storage__ConversationTableName` | `TimesheetAgentConversation` |

Секреты и API keys не добавлялись. Существующая настройка `AZURE_CLIENT_ID` подтверждена как client
ID `id-internal-gtimesheet-test`. Изменение app settings автоматически перезапустило test App Service;
после перезапуска Azure сообщил `Running` и `Normal`. Production и APIM не изменялись.

Последний код message endpoint и Foundry orchestration на этом шаге не развёртывался, поэтому реальный
inference и запись истории должны проверяться после следующего CI/CD deployment API.

Для production необходимо отдельно выбрать регион и deployment type, проверить квоту и требования
data residency, создать production deployment и назначить минимальные роли только production identity.

## 28.09.2026 — ZIP deployment Telegram-бота и message operation в APIM

В test Function App `func-internal-gtimesheet-test` напрямую развёрнут ZIP-пакет Telegram-бота из
коммита `9ac5a3c Add agent text message handling`.

Перед deployment выполнены Release-сборка и все 7 тестов бота; тесты прошли. Пакет создан во временной
директории вне Git-репозитория, поэтому локальный `launchSettings.json` и содержащиеся в нём секреты в
ZIP не вошли. Deployment выполнен через Azure CLI без remote build:

- deployment ID: `3b495a76-2cdf-4048-9cbd-b63fc7d57ba1`;
- итоговый статус: `4` / successful;
- Function App после recycle: `Running`, availability `Normal`;
- Azure обнаружил функции `HandleBotEntity`, `HandleBotHttp` и `HealthCheck`.

Существующие App Settings, Managed Identity и Telegram webhook deployment-командой не изменялись.
Прямой вызов Function App `/health` вернул `401`, что соответствует защищённому внешнему контуру.

В test APIM `apim-integration-platform-test-01`, API `garage-timesheet-agent-api`, добавлена операция:

| Параметр | Значение |
|---|---|
| Operation ID | `post-agent-message` |
| Method | `POST` |
| URL template | `/internal/agent/messages` |

У диагностической операции `/internal/agent/profile` нет отдельной operation policy: backend URL и
client certificate настроены общей policy API. Поэтому новая операция наследует тот же защищённый
маршрут; JWT заголовок бота не подменяется. Общая policy, Mini App API и production не изменялись.

Откат бота: повторно развернуть предыдущий ZIP/коммит. Откат APIM: удалить только operation
`post-agent-message`. Для production операцию необходимо добавить декларативно через принятую IaC или
CI/CD-схему одновременно с production deployment бота и API.

### Исправление маршрутизации обычного текста

Первая Telegram-проверка показала, что обычный вопрос пользователя вызвал диагностический
`POST /internal/agent/profile`, а не `POST /internal/agent/messages`. Application Insights подтвердил
успешный profile-запрос и отсутствие message-запроса.

Причина была в коде бота: `AgentProfileCommand` одновременно регистрировался как именованная команда
`profile` и реализовывал общий `IChatCommandParser` с безусловно успешным `Parse`. В результате он
перехватывал обычные сообщения раньше agent message parser.

Profile-команда оставлена для демонстрации, но исключена из общей parser-цепочки: теперь она доступна
только как `/profile`. Исправленный пакет повторно развёрнут ZIP-способом:

- deployment ID: `c201a2f2-6cc7-4b88-a646-283368b4a28f`;
- итоговый статус: `4` / successful;
- изменения Azure-конфигурации при повторном deployment не выполнялись.

Добавлен regression-тест, проверяющий, что `AgentProfileCommand` больше не является fallback parser.
После исправления прошли все 8 тестов бота. Код и тест пока не закоммичены.

После повторной отправки обычного текста webhook `HandleBotHttp` успешно завершился с HTTP 204 и
Durable sidecar принял `SignalEntity`, однако `HandleBotEntity` не запустился. Вызовов APIM, agent API
и Foundry для этого update не было; поэтому отсутствие ответа не связано с моделью или message endpoint.

После двух последовательных ZIP recycle Durable worker сохранил pending signal, но не выбрал его из
control queue. Test Function App был штатно перезапущен через Azure CLI; состояние после перезапуска —
`Running` / `Normal`. Настройки и Durable Storage не очищались. Конфигурация показывает control queue
visibility timeout 5 минут, поэтому исходный сигнал может быть повторно обработан после окончания
невидимости. Production не изменялся.

После перезапуска новый Telegram update был успешно обработан. Пользователь получил содержательный
ответ на read-only вопрос о доступных проектах. Это подтвердило фактическую цепочку Telegram bot →
Managed Identity → APIM → Timesheet API → user binding → Semantic Kernel → Foundry `gpt-5-mini` →
project tool → Telegram response. Дополнительные Azure-изменения для успешной проверки не выполнялись.

Исправление маршрутизации бота зафиксировано коммитом `eace3c6 Fix agent message command routing`.

Исправление binding дат `14ecd63 Fix timesheet tool date binding` развёрнуто штатным CI/CD API и
успешно проверено реальным Telegram-запросом чтения списаний за текущий день. Дополнительные Azure,
APIM, Entra ID, Foundry или Storage изменения для исправления не потребовались.

## Правила дальнейшего ведения

После каждого изменения Azure необходимо до завершения инкремента записать:

1. дату, среду, подписку, resource group и точное имя ресурса;
2. исходное и итоговое состояние без публикации секретов;
3. причину изменения и связанный симптом/решение;
4. способ изменения: Portal, Azure CLI, CI/CD, ARM/Bicep/Terraform либо прямой deployment;
5. результат проверки и способ отката;
6. соответствующий Git commit, если изменение связано с кодом;
7. отдельный production checklist, если конфигурацию потребуется воспроизвести.

Секретные значения разрешено упоминать только по имени настройки или объекта Key Vault. Их значения в Git не добавляются.

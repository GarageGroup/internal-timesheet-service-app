# Telegram AI Agent: журнал изменений Azure

Последнее обновление: 05.10.2026.

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

## 29.09.2026 — подготовка Action Table и выключенных write-настроек

Среда: test. Подписка: `73a6f94e-bfb4-4926-a9dd-09228d53a2a5`.

В существующем Storage Account `stinternalgtimesheettest` создана Azure Table
`TimesheetAgentAction`. Таблица предназначена для подготовленных агентом действий, ожидающих
подтверждения пользователя, и их дальнейших состояний. Новые Storage Account, Managed Identity и
RBAC assignments не создавались: API уже имеет `Storage Table Data Contributor` на этом Storage
Account.

Изменение выполнено через Azure CLI с Entra-аутентификацией:

```powershell
az storage table create `
  --account-name stinternalgtimesheettest `
  --name TimesheetAgentAction `
  --auth-mode login
```

В App Service `app-garage-timesheet-service-test`, resource group
`rg-garage-timesheet-test`, добавлены настройки:

| Настройка | Значение |
|---|---|
| `Agent__Storage__ActionTableName` | `TimesheetAgentAction` |
| `Agent__WritePreparation__Enabled` | `false` |
| `Agent__WritePreparation__ApprovalTtlMinutes` | `10` |
| `Agent__WritePreparation__ProjectSearchTop` | `20` |

Настройки применены через Azure CLI. Изменение App Settings вызывает recycle приложения; после
применения App Service проверен в состоянии `Running`. Таблица повторно найдена через list-команду,
а четыре настройки прочитаны из фактической конфигурации приложения.

Feature flag оставлен выключенным намеренно. Текущее пользовательское поведение агента остаётся
read-only; подготовка списания, подтверждение и запись в CRM этим изменением не активированы. APIM,
Telegram-бот, Foundry, Entra ID и production не изменялись.

Связанный код Application composition и feature flag находится в коммите
`eed96a2 Wire write preparation behind feature flag`. На момент инфраструктурной подготовки его
развёртывание через CI/CD отдельно не выполнялось.

Откат:

1. Оставить `Agent__WritePreparation__Enabled=false` либо удалить все четыре добавленные настройки.
2. Удалять `TimesheetAgentAction` только после проверки отсутствия нужных записей; удаление таблицы
   необратимо для содержащихся в ней данных.
3. Для production создать отдельную таблицу в production Storage Account, назначить production API
   минимальную роль `Storage Table Data Contributor` и сначала применить flag со значением `false`.

## 29.09.2026 — включение и smoke-тест write preparation в test

После успешного CI/CD deployment коммита `0e9a6b4` в App Service
`app-garage-timesheet-service-test` настройка `Agent__WritePreparation__Enabled` изменена с `false`
на `true` через Azure CLI. Изменение App Settings вызвало recycle; после него приложение проверено в
состоянии `Running`. Другие App Settings, APIM, Managed Identity, RBAC, Foundry, Entra ID и production
не изменялись.

Для проверки через реальный контур бота сформированы тестовые Telegram updates и переданы напрямую
в защищённую функцию `HandleBotHttp` с Function key. Персональные Telegram ID, Function key и
Storage key не выводились и не сохранялись. Дальнейший путь оставался штатным: Durable Entity,
Managed Identity бота, APIM, agent API, Semantic Kernel, Foundry и Telegram send API.

Проверено:

- read-only запрос завершился через `/internal/agent/messages` с HTTP 200, а отправка ответа в
  Telegram — с HTTP 200;
- корректный запрос создал одну запись в `TimesheetAgentAction` со статусом `Pending`, каноническим
  проектом, датой `2026-09-29`, длительностью `0.5`, исходным комментарием и TTL 10 минут;
- повторное чтение фактических списаний подтвердило, что подготовленное действие не появилось в
  Dataverse;
- неизвестный проект и пустой комментарий не создали новых action;
- запрос двух списаний за один model turn создал только первое действие; второе не было сохранено;
- после теста в таблице находились две подготовленные записи, обе в статусе `Pending`; функций
  confirm/execute в развёрнутой версии нет.

Feature flag оставлен равным `true` для следующего этапа разработки callback. Текущий риск ограничен
test-средой: агент может создавать только истекающие preview-записи в Table Storage и не может
выполнять CRM write.

Во время проверки обнаружено, что текущая HTTP-телеметрия бота записывает полный URL исходящего
Telegram Bot API запроса. Поскольку токен является частью Telegram URL, секрет попадает в trace
Application Insights. Значение секрета в документацию не переносилось. До production необходимо
настроить redaction/suppression таких URL и ротировать токен после исправления логирования. Это
отдельная security-задача; конфигурация логирования и токен в данном инкременте не изменялись.

Откат write preparation: установить `Agent__WritePreparation__Enabled=false`. Удалять тестовые
`Pending`-записи для отката не требуется: без confirm/execute они не могут изменить Dataverse.

## 29.09.2026 — APIM operation подтверждения agent action

В test APIM `apim-integration-platform-test-01` в subscription
`106dd084-8190-453f-87c9-cd2cb714b1d6`, API `garage-timesheet-agent-api`, добавлена операция:

| Параметр | Значение |
|---|---|
| Operation ID | `post-agent-action-decision` |
| Display name | `Decide agent action` |
| Method | `POST` |
| URL template | `/internal/agent/actions/{actionId}/decision` |
| Template parameter | `actionId`, required, string |

Отдельная operation policy не добавлялась. Операция наследует общую policy `garage-timesheet-agent-api`:
backend test Timesheet API, backend client certificate и неизменённый Bearer JWT Managed Identity бота.
Существующие profile/message operations, Mini App API, App Service, Function App, Entra ID, Storage,
Foundry и production не изменялись.

На момент создания operation новый API endpoint ещё не развёрнут в test App Service, поэтому
сквозной вызов не выполнялся. До deployment API маршрут может закономерно вернуть backend `404`.

Откат: удалить только operation `post-agent-action-decision` из API `garage-timesheet-agent-api`.

Для production: добавить эту operation декларативно в production agent API после/вместе с deployment
endpoint, сохранить обязательный route parameter и убедиться, что общая policy предъявляет production
backend certificate и не удаляет Bearer header.

## 29–30.09.2026 — ZIP deployment бота с inline-подтверждением

В test Function App `func-internal-gtimesheet-test` напрямую развёрнут ZIP-пакет Telegram-бота из
коммита `239f29f Add Telegram agent action confirmation`.

Перед deployment выполнены Release-сборка и все 17 тестов бота; тесты прошли. Пакет создан из
`dotnet publish` во временной директории вне Git-репозитория. До архивации проверено отсутствие
`launchSettings.json` и `local.settings.json`; локальные Telegram-секреты в ZIP не вошли.

Результат deployment:

| Параметр | Значение |
|---|---|
| Deployment ID | `11fc58dd-c1c8-427b-9b68-c070c6cfd664` |
| Status | `4` / successful |
| Function App state | `Running` |
| Availability | `Normal` |
| Обнаруженные функции | `HandleBotEntity`, `HandleBotHttp`, `HealthCheck` |

После ZIP deployment Function App один раз перезапущен через Azure CLI без изменения App Settings.
Причина: первый update ожидал запуска Durable Entity около 82 секунд и потерял оставшийся бюджет
выполнения; вызов agent API завершился `499` во время обращения к Foundry. После restart последующие
два agent message вызова завершились HTTP 200 и создали два тестовых `Pending` action.

Проверено:

- новый APIM decision route без JWT возвращает `401`, то есть публичный обход защиты отсутствует;
- два prepare-запроса прошли через webhook, Durable Entity, Managed Identity, APIM, API, Semantic
  Kernel и Foundry;
- оба action имеют проект `Test 01`, длительность `0.5` и состояние `Pending`; Dataverse write до
  подтверждения не выполнялся;
- Telegram API за период проверки принял семь вызовов с HTTP 200, включая agent-ответы;
- синтетические callback updates были приняты ingress с HTTP 204, но не дошли до decision endpoint
  и не изменили состояние actions. Они не считаются успешной проверкой callback; требуется реальное
  нажатие Telegram-кнопки пользователем;
- при формировании первого тестового сообщения PowerShell отправил русский текст с неверной
  кодировкой. Это дефект локального smoke-скрипта, а не приложения; следующие JSON body передавались
  как UTF-8 bytes.

App Settings, Managed Identity, RBAC, Entra ID, Storage schema, Foundry, webhook, API App Service и
production не изменялись. Два тестовых action необходимо отменить кнопками либо дождаться их TTL;
повторно подтверждать их нельзя.

Откат: повторно выполнить ZIP deployment предыдущего bot package. Restart не требует отдельного
отката. Для production использовать штатный CI/CD deployment и отдельно проверить cold start Durable
Entity до включения write feature flag.

### Исправление строкового enum в decision response

Первое реальное нажатие «Отменить» вызвало decision endpoint с HTTP 200 и корректно перевело action
в `Cancelled`, но бот показал общий текст ошибки. Application Insights зафиксировал
`System.Text.Json.JsonException` на `$.decision`: API сериализует enum ответа строкой (`Cancel`), а
клиент бота ожидал стандартное числовое представление.

В `AgentActionApi` добавлены отдельные настройки только для десериализации response с
`JsonStringEnumConverter`. Сериализация request не менялась: уже проверенный числовой `decision`
сохранён. Unit-тест изменён так, чтобы использовать фактическое строковое представление ответа.

Исправленный бот повторно развёрнут ZIP-пакетом:

| Параметр | Значение |
|---|---|
| Deployment ID | `bc528e4e-75cf-4335-81a4-d1e124ee2a1e` |
| Status | `4` / successful |
| Function App state | `Running` / `Normal` |

После deployment test Function App один раз перезапущен без изменения настроек. Повторная реальная
отмена свежего action завершилась HTTP 200; action перешёл `Pending → Cancelled`, бот ответил
«Списание отменено.», исключения в коррелированной операции отсутствуют. Dataverse write не
выполнялся.

Production и остальные Azure-ресурсы не изменялись. Для production исправление должно попасть в
обычный bot artifact; дополнительных настроек или ресурсов не требуется.

### Обработка Problem Details decision endpoint

Для диагностики и исправления пользовательских сообщений выполнены последовательные ZIP deployment
только test Function App `func-internal-gtimesheet-test`:

| Deployment ID | Результат |
|---|---|
| `b11892e2` | первоначальный разбор стандартных `title/detail` |
| `39512f51` | проверка гипотезы о строковом `failureCode` |
| `4d1db760-9942-489d-9d2d-c9a48c302ba9` | проверка числового `failureCode` |
| `e37e4fa2-a6cb-4a69-bf74-9491177181cd` | итоговый разбор фактического `detail`; status `4` / successful |

Проверка сгенерированного кода endpoint framework показала, что ответ имеет поля `type`, `title`,
`status`, `detail`; смысловая безопасная причина находится в `detail`, а `failureCode` не публикуется.
Итоговый реальный callback вернул безопасное сообщение о некорректных параметрах списания, удалил
inline-клавиатуру и оставил action в `Failed`. Dataverse write не выполнялся.

App Settings, Managed Identity, RBAC, Entra ID, APIM policies/routes, Storage schema, Foundry и webhook
не изменялись. Production не затронут. Для переноса на production нужен только обычный bot artifact;
отдельные Azure-настройки не требуются. Откат — ZIP/CI deployment предыдущего bot artifact.

### Deployment локализованного agent UI

Коммит бота `8eaad61` развёрнут прямым ZIP deployment в test Function App
`func-internal-gtimesheet-test`:

| Параметр | Значение |
|---|---|
| Deployment ID | `45da0db1-9ccc-450e-bd02-bee00fc2fce7` |
| Status | `4` / successful |
| Итоговое состояние | `Running` / `Normal` |

Первый `/profile` был принят ingress, но `HandleBotEntity` попал в момент замены сборок работающего
процесса и завершился ошибкой metadata token в `ChatContext.GetLocalizer`. Выполнен один restart
только test Function App. App Settings и остальные Azure-ресурсы не менялись.

После restart повторный `/profile` успешно прошёл через `HandleBotHttp` и `HandleBotEntity`.
Telegram update намеренно содержал язык `en`, профиль — `ru`; бот вернул русский текст, подтвердив
приоритет языка профиля. Для production локализация переносится обычным bot artifact. После прямого
ZIP deployment рекомендуется контролируемый restart до приёма update либо deployment slot/swap,
чтобы исключить обработку Durable Entity во время замены DLL. Откат — deployment предыдущего
артефакта бота; отдельный откат конфигурации не требуется.

### Сквозной Confirm smoke test

Первое подтверждение action на московскую дату `2026-09-30` было отклонено существующим Dataverse
create с HTTP 400, потому что в UTC ещё было `2026-09-29`. Action корректно перешёл в `Failed`, запись
не создавалась. Azure-конфигурация для этого результата не менялась.

Повторный контролируемый action использовал дату `2026-09-29`, проект `Test 01`, длительность `0.1`
часа и уникальный тестовый комментарий. Реальный Telegram callback прошёл по всей цепочке:

- APIM decision endpoint: HTTP 200;
- Dataverse project check: HTTP 200;
- Dataverse `POST gg_timesheetactivities`: HTTP 204;
- Action Table: конечное состояние `Succeeded`;
- Telegram: ответ «Время успешно списано.»;
- Telegram inline-клавиатура после успешного решения удалена;
- коррелированные исключения: 0.

Создана одна реальная тестовая запись в test Dataverse за `2026-09-29`. Production и Azure-ресурсы
не изменялись. Для production до запуска необходимо согласовать единое правило бизнес-даты и часового
пояса между prompt агента, API и Dataverse validation; изменение одного только prompt может скрыть,
но не устранить расхождение.

### Deployment Telegram UI для update

После пользовательского CI/CD deployment API бот из коммита `93a60f0` развёрнут прямым ZIP
deployment только в test Function App `func-internal-gtimesheet-test`.

| Параметр | Значение |
|---|---|
| Subscription | `73a6f94e-bfb4-4926-a9dd-09228d53a2a5` |
| Resource group | `rg-garage-timesheet-test` |
| Deployment ID | `06641d95-8a4f-43ef-a6c8-0b779487842a` |
| Status | `4` / successful |
| Итоговое состояние | `Running` / `Normal` |

Release-пакет перед отправкой проверен на наличие русской satellite assembly. После deployment без
ручного restart доступны `HandleBotEntity`, `HandleBotHttp` и `HealthCheck`. Затем пользователь
сквозно подтвердил update Cancel и Confirm: отмена не изменила Dataverse, подтверждение применило
сохранённый целевой snapshot, клавиатура исчезла в обеих ветках.

App Settings, Managed Identity, RBAC, Entra ID, APIM, Storage, Foundry, webhook и production не
изменялись. Для production требуется обычный согласованный deployment API и bot artifacts; новых
ресурсов именно для update UI не требуется. Откат — deployment предыдущего bot artifact и отключение
`Agent__WritePreparation__Enabled`, без удаления Action Table.

## 01.10.2026 — удаление диагностической operation

После удаления диагностического `/profile` из исходного кода API и Telegram-бота из test APIM удалена устаревшая operation:

| Параметр | Значение |
|---|---|
| Subscription | `106dd084-8190-453f-87c9-cd2cb714b1d6` |
| APIM | `apim-integration-platform-test-01` |
| API ID | `garage-timesheet-agent-api` |
| Operation ID | `get-agent-profile` |
| Method и route | `POST /internal/agent/profile` |

После удаления повторно прочитан список operations. В agent API остались `post-agent-message` и `post-agent-action-decision`; их method и URL template не изменились. Общая API policy, backend certificate, App Registration, Managed Identity, роли, App Service, Function App и production не изменялись. Для production диагностическую operation создавать не нужно.

## 01.10.2026 — test deployment распознавания голоса

В существующем Azure AI Services resource создан отдельный audio-to-text deployment:

| Параметр | Значение |
|---|---|
| Subscription | `73a6f94e-bfb4-4926-a9dd-09228d53a2a5` |
| Resource group | `rg-garage-timesheet-test` |
| Resource | `ai-timesheet-test` |
| Region | `West Europe` |
| Deployment | `whisper` |
| Model | `OpenAI/whisper`, version `001` |
| SKU | `Standard`, capacity `1` |
| Итоговое состояние | `Succeeded` |

Перед созданием через Azure CLI подтверждено, что `whisper` поддерживает `audioTranscriptions`, а в регионе доступна Standard quota. Для API используется endpoint `https://ai-timesheet-test.openai.azure.com/`. Существующая User Assigned Managed Identity `id-internal-gtimesheet-test` уже имела `Cognitive Services User` на `ai-timesheet-test`; новые identity и role assignment не создавались.

В test App Service `app-garage-timesheet-service-test` добавлены настройки:

- `Agent__Voice__Enabled=false`;
- `Agent__Voice__Endpoint=https://ai-timesheet-test.openai.azure.com/`;
- `Agent__Voice__DeploymentName=whisper`;
- `Agent__Voice__ModelId=whisper`;
- `Agent__Voice__MaxFileSizeBytes=5242880`.

В test Function App `func-internal-gtimesheet-test` добавлена совпадающая предварительная настройка `AgentVoice__MaxFileSizeBytes=5242880`. Изменение перезапустило только test Function App; итоговое состояние также проверено как `Running` / `Normal`.

Voice feature API оставлен выключенным до deployment API и Telegram-бота. APIM, App Registration, Storage, Dataverse и production не менялись.

Связанный API commit: `55d410a`. Проверка реального audio inference будет выполнена после deployment кода. Быстрый rollback: оставить/вернуть `Agent__Voice__Enabled=false`; полный rollback после проверки отсутствия consumers — удалить deployment `whisper` и пять Voice settings.

### ZIP deployment Telegram-бота

После commit бота `8768c8a` выполнен ZIP deployment в test Function App `func-internal-gtimesheet-test`. Azure deployment `1acb5601-a43f-4734-b72d-ed2056701197` завершён 01.10.2026 со статусом `4` (success); приложение проверено в состоянии `Running` / `Normal`. Production не затронут.

Во время deployment Azure CLI показал предупреждение о завершении поддержки выбранного `dotnet-isolated` runtime 10.11.2026. До production rollout необходимо отдельно проверить актуальный runtime stack Function App и выполнить поддерживаемое обновление; автоматическое изменение runtime в рамках voice deployment не выполнялось.

На момент deployment бота `Agent__Voice__Enabled=false` в API, поэтому новый код безопасно развёрнут без активации голосовых запросов. Включение выполняется отдельно после завершения deployment API и проверки его состояния.

### Включение voice feature после deployment API

После подтверждения успешного deployment API настройка `Agent__Voice__Enabled` в test App Service `app-garage-timesheet-service-test` изменена с `false` на `true`. Значение повторно прочитано из App Settings; App Service находится в состоянии `Running` / `Normal`. Production не изменялся.

Прямой запрос к `/health` вернул `403 Client Certificate Required`. Это ожидаемая проверка сохранности сетевой границы: backend API не принимает прямой внешний запрос без клиентского сертификата APIM. Сквозная проверка voice выполняется через Telegram-бота и существующий APIM route.

### Исправление скачивания Telegram voice

Первый voice smoke test завершился локализованной ошибкой скачивания. По телеметрии `getFile` через APIM был успешен; причиной оказалось отсутствующее значение `Bot__FileUrlTemplate`: Telegram-движок формировал пустой `FileUrl` из полученного `file_path`.

Подтверждено, что в существующем `gtimesheet-telegram-api` уже есть operation `GET /{type}/{name}` (`get-file`), policy которой безопасно подставляет Telegram bot token внутри APIM. Новые APIM operation и policy не создавались.

В test Function App добавлена настройка:

- `Bot__FileUrlTemplate=https://apim-garage-timesheet-test.azure-api.net/telegram/api/{0}`.

Загрузчик бота изменён так, чтобы передавать существующий `TelegramBot:ApiKey` как `Ocp-Apim-Subscription-Key` при скачивании через этот маршрут. Telegram bot token в Function App не добавлялся.

Первый повторный ZIP-запрос получил transient `502` и не создал deployment. Попытка через OneDeploy создала неактивный deployment `cbab22f4-0cd0-4bd1-b534-edc89cdee710` со статусом `3`: платформа ошибочно запустила Oryx build для готового publish package. Рабочая версия приложения при этом не менялась. Повторный ZIP deployment `cedc7b46-ff38-458b-a333-da1b85f3ba29` завершился со статусом `4` и стал active; Function App и три функции проверены.

Повторный сквозной voice smoke test успешен: Telegram-файл скачан через APIM, Whisper transcription и агентский read-only запрос выполнены, preview write-операции отображён, кнопки Cancel и Confirm отработали корректно.

Первый запрос после перезапуска не успел завершиться: backend зарегистрировал `499` примерно через 25 секунд, хотя скачивание файла, Whisper и первый вызов chat model завершились с `200`. После прогрева Managed Identity токенов повторный запрос прошёл. Изменение APIM timeout не выполнялось. Перед production и при следующей работе с integration APIM необходимо проверить effective `forward-request` timeout и обеспечить запас для cold-start цепочки `identity → transcription → agent/tools`.

### Timeout agent message operation

По решению владельца test-контура для единственной operation `post-agent-message` в API `garage-timesheet-agent-api` добавлена operation-level policy:

```xml
<backend>
  <forward-request timeout="60" />
</backend>
```

Scope изменения: subscription `106dd084-8190-453f-87c9-cd2cb714b1d6`, resource group `rg-integration-platform-test`, APIM `apim-integration-platform-test-01`, API `garage-timesheet-agent-api`, operation `POST /internal/agent/messages`. API-level inbound policy с backend URL и сертификатом продолжает наследоваться через `<base />`. Operation решения по `ActionId`, остальные API и production не изменялись. Policy повторно прочитана из Azure с `timeout="60"`. Rollback — удалить только operation policy `post-agent-message`, вернув наследуемое поведение.

После уточнения smoke test установлено, что повторно отправленное пользователем голосовое сообщение являлось отдельным Telegram update: после ожидания бот прислал два ответа на два фактически отправленных сообщения. Это не подтверждает повторную доставку одного update; существующая идемпотентность по `TelegramUpdateId` не изменялась.

### ZIP deployment улучшенного Telegram UI

01.10.2026 в test Function App `func-internal-gtimesheet-test` из resource group `rg-garage-timesheet-test` развёрнут bot commit `8bd8263`. Способ — Azure CLI ZIP deployment готового Release publish package; deployment ID `8de7f595-f4f4-4af2-be43-10e7c2b1ecf1`, итоговый статус `4` (`Success`, active).

Перед загрузкой проверено наличие русской satellite assembly. После deployment Function App имеет состояние `Running`, availability `Normal`; функции `HandleBotEntity`, `HandleBotHttp` и `HealthCheck` обнаруживаются платформой. App settings, Managed Identity, App Registration, RBAC, APIM policies и production-ресурсы не изменялись.

Связанный API commit `8ccc092` разворачивается отдельно пользовательским CI/CD. До завершения API deployment новый bot/API contract нельзя считать сквозно проверенным. Rollback бота — повторный ZIP deployment publish package предыдущего bot commit. Azure CLI повторно предупредил об окончании поддержки текущего `dotnet-isolated` runtime 10.11.2026; задача обновления runtime уже остаётся обязательной до production.

Первый read-only smoke test показал буквальные теги `<code>`: команда уже задавала Telegram `ParseMode=Html`, но затем целиком применяла `HtmlEncode` к тексту модели. В локальном hotfix убрано повторное кодирование всего ответа; вместо него добавлен allowlist-sanitizer для `<b>`, `<i>` и `<code>`, а остальной текст и HTML безопасно кодируются. Preview write-операций продолжает отдельно экранировать динамические значения. Промежуточный deployment `0e745213-7022-4d62-90b6-9871e8b29279` заменён итоговым ZIP deployment `0f833a53-e63b-4309-b074-a37375c9fc9d`, статус `4`, active. Function App после deployment находится в `Running/Normal`, три функции обнаруживаются. Настройки Azure и APIM не менялись; hotfix пока не закоммичен.

## 02.10.2026 — подготовка управляемого CI/CD без применения Azure

В репозитории подготовлены Bicep и workflows для управления Timesheet API infrastructure. Никакие
Azure, Entra ID, Dataverse, APIM или GitHub settings этим инкрементом не изменялись.

Выполнен read-only `what-if` для test resource group. Первая попытка с RBAC resources ожидаемо была
отклонена из-за отсутствия у текущего пользователя `Microsoft.Authorization/roleAssignments/write`;
никакой deployment не создавался. Повторный preview с `manageRoleAssignments=false` успешно проверил
остальной шаблон и не показал Create/Delete/Replace существующих Timesheet resources.

Перед первым запуском `install.yml` для Test обязательны:

1. Выполнить административный shell-скрипт из `.infra/README.md` для Test.
2. Заполнить GitHub Environment `Test` по `.infra/README.md`.
3. Учесть ожидаемые изменения: `allowBlobPublicAccess=false`, `ftpsState=Disabled`,
   `http20Enabled=true` и связь Application Insights с управляемым Log Analytics Workspace.
4. Перед запуском сверить параметры GitHub Environment с фактическими именами ресурсов.

Откат не требуется, поскольку Azure не менялся. После будущего запуска каждый фактический deployment,
resource ID, role assignment и результат smoke test записываются отдельной датированной секцией.

После сверки с эталонным `internal-exchange-rates-app` из workflow удалены не предусмотренные образцом
режимы `WhatIf/Apply`. `install.yml` сразу выполняет `az deployment group create --mode Incremental`.
Это изменение затрагивает только незакоммиченные файлы CI/CD; Azure по-прежнему не изменялся.

Из API pipeline также удалено назначение `Timesheet.Agent.Invoke` Managed Identity Telegram-бота.
API pipeline владеет App Registration агента и объявлением application role, а assignment конкретному
потребителю должен выполнять CI/CD бота. Из bootstrap API удалено ставшее лишним Microsoft Graph
разрешение `AppRoleAssignment.ReadWrite.All`. Изменения существуют только локально, Azure не менялся.

Одноразовый административный bootstrap перенесён целиком в `.infra/README.md`. Отдельный
`.infra/scripts/bootstrap-github-oidc.sh` удалён как неиспользуемый workflow-файл; логика выдачи
OIDC/RBAC/Graph permissions сохранена в копируемом shell-блоке документации. Azure не изменялся.

Контракт GitHub Environment упрощён: удалены `MANAGE_ROLE_ASSIGNMENTS`, `IDENTITY_LOCATION`,
`APP_SERVICE_PLAN_SKU_TIER` и отдельные параметры Foundry для upgrade policy, RAI policy и local auth.
RBAC assignments теперь создаются всегда; фиксированные Foundry-настройки заданы в Bicep. Добавлена
одна переменная `FOUNDRY_LOCATION` для создания Foundry account/project. Существующие локации MI и
Foundry account install-скрипт сохраняет автоматически, не сравнивая их с переменными создания.
Azure этим изменением не затрагивался.

После подготовки CI/CD Telegram-бота интеграционная ответственность скорректирована повторно, чтобы
исключить циклический первый запуск. Штатный порядок: сначала bot pipeline создаёт Function App и
System Assigned MI, затем API pipeline создаёт agent App Registration/role, назначает роль MI бота и
заполняет bot `AgentApi` settings. В API bootstrap возвращено `AppRoleAssignment.ReadWrite.All`.
Изменения существуют только в незакоммиченных CI/CD-файлах; Azure не менялся.

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
# Планируемое изменение CI/CD от 05.10.2026

Для устранения цикла первого запуска в новой Resource Group подготовлены изменения в репозиториях API и бота. API `install` создаёт отдельную UAMI `<имя Function App бота>-agent`, назначает ей Entra app role `Timesheet.Agent.Invoke` и разрешает её client ID в Agent API. Bot `install` после API подключает UAMI к Function App и заполняет `AgentApi__Audience`, `AgentApi__BaseAddress`, `AgentApi__ManagedIdentityClientId`. Код бота выбирает эту UAMI для токена Agent API; Dataverse UAMI остаётся отдельной. На 05.10.2026 эти изменения в Azure не применялись. После развёртывания следует проверить app role assignment, список identity Function App, настройки API и бота и успешный агентский вызов.

В тот же набор изменений добавлена автоматизация первичного развёртывания кода и маршрутов. API pipeline создаст Timesheet API, Agent API, health/Swagger operations и scoped subscriptions в существующем общем APIM, при необходимости импортирует backend PFX из GitHub secrets. Bot pipeline создаст отдельный Consumption APIM в Resource Group приложения, настроит webhook `/bot/message`, прокси `/telegram/api`, именованные значения и подписки. Оба pipeline создадут Blob Container в существующем общем Storage Account, запишут bootstrap ZIP и развернут его. Новые ресурсы и настройки ещё не применялись; планируемые изменения Test должны быть проверены перед деплоем.

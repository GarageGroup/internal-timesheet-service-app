# Telegram AI Agent: журнал изменений Azure

Последнее обновление: 26.09.2026.

Этот документ фиксирует инфраструктурные изменения тестового контура Telegram AI Agent. Он предназначен для аудита и последующего воспроизведения конфигурации в production. Секреты, ключи, токены, connection strings и персональные данные здесь не публикуются.

Связанные документы:

- [план реализации](telegram-ai-agent-implementation-plan.md);
- [журнал прогресса](telegram-ai-agent-progress.md).

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

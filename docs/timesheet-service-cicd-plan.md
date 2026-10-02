# План перевода Timesheet API на управляемый CI/CD

## 1. Цель

Собрать воспроизводимый CI/CD для `internal-timesheet-service-app`, который:

- собирает и тестирует решение на .NET 10;
- проверяет Bicep и shell-скрипты до merge;
- создаёт и обновляет принадлежащую API Azure-инфраструктуру;
- идемпотентно настраивает Entra ID, Azure RBAC, Dataverse и APIM;
- публикует неизменяемый ZIP-артефакт релиза в Azure Blob Storage;
- разворачивает в Test именно ZIP, сохранённый в Blob Storage;
- позволяет развернуть ту же версию в Prod без повторной сборки;
- сохраняет обязательный job `update-swagger-test` после успешного health check;
- не переносит test GUID, URL, identity или секреты в Prod.

В качестве структурного образца используется `internal-exchange-rates-app`, но инфраструктура остаётся
App Service, а не Azure Functions: Timesheet API — ASP.NET Core Web App.

## 2. Граница владения

### Ресурсы Timesheet API, которыми управляет этот репозиторий

- resource group среды, если её ещё нет;
- Linux App Service Plan;
- Linux Web App `app-...-timesheet-service-<environment>` с .NET 10;
- user-assigned Managed Identity API;
- Log Analytics Workspace и Application Insights;
- Storage Account агента либо явно выбранный application Storage Account;
- таблицы `TimesheetAgentConversation` и `TimesheetAgentAction`;
- Azure AI Foundry/Azure AI Services resource, project и deployments:
  - chat model, сейчас `gpt-5-mini`;
  - audio-to-text model, сейчас `whisper`;
- Azure RBAC для UAMI API:
  - `Storage Table Data Contributor`;
  - `Cognitive Services User`;
- App Settings API, кроме значений секретов в открытом виде;
- Entra App Registration agent API, service principal, application role
  `Timesheet.Agent.Invoke` и назначение этой роли Managed Identity Telegram-бота;
- Dataverse Application User для UAMI API и назначение прикладной security role;
- принадлежащие Timesheet операции, backend и policies в существующем APIM.

### Общие ресурсы, которые не должны пересоздаваться

- корпоративный APIM service;
- Dataverse environment;
- Telegram Function App и её system-assigned Managed Identity;
- общий Storage Account/контейнер релизных артефактов;
- backend certificate или Key Vault certificate, управляемый integration platform;
- GitHub Environments и deployment App Registrations.

Pipeline должен получать идентификаторы этих ресурсов из Environment variables, настраивать только
Timesheet-часть и завершаться ошибкой, если обязательный общий ресурс не найден.

## 3. Целевая структура репозитория

```text
.github/workflows/
  build.yml
  install.yml
  publish.yml
  deploy.yml
  delete.yml

.infra/
  README.md
  main.bicep
  modules/
    app-service.bicep
    observability.bicep
    storage.bicep
    foundry.bicep
    role-assignments.bicep
  apim/
    main.bicep
    policies/
      mini-app-api.xml
      agent-api.xml
      agent-message.xml
  scripts/
    install-azure-resources.sh
    configure-entra-agent-api.sh
    grant-dataverse-managed-identity-access.sh
    configure-apim.sh
    set-web-app-settings.sh
    validate-deployment.sh
```

Файлы могут быть объединены, если маленький модуль не улучшает читаемость. Логическое разделение
App Service, Storage, Foundry, RBAC, Entra, Dataverse и APIM сохраняется обязательно.

## 4. Workflows

### `build.yml`

Запускается на push во все ветки и на pull request.

1. Checkout.
2. Setup .NET `10.0.x`.
3. `az bicep build` для всех entry-point Bicep-файлов.
4. `bash -n` для всех `.infra/scripts/*.sh`.
5. Restore `Internal.Timesheet.Service.slnx`.
6. Release build без повторного restore.
7. Запуск всех тестов через Microsoft Testing Platform.
8. Проверка отсутствия устаревших и известных уязвимых пакетов может быть отдельным информативным
   шагом; build не должен становиться нестабильным из-за временной недоступности advisory feed.

### `install.yml`

Ручной запуск с Environment `Test` или `Prod` и отдельными skip-флагами.

1. OIDC login в Azure без client secret.
2. Проверка provider registrations, location и входных параметров.
3. Создание/обновление resource group и application resources через Bicep в `Incremental` mode.
4. Создание таблиц агента.
5. Создание UAMI API и назначение её Web App.
6. Назначение Storage/Foundry RBAC.
7. Создание/обновление Foundry project и двух model deployments с параметризованными model version,
   SKU и capacity.
8. Идемпотентная настройка Entra App Registration, app role, service principal и role assignment MI
   Telegram-бота.
9. Идемпотентная регистрация UAMI API как Dataverse Application User и назначение согласованной роли.
10. Создание/обновление Timesheet-конфигурации в существующем APIM.
11. Запись несекретных outputs и итоговая read-only валидация.

Feature flags `Agent__Enabled`, `Agent__WritePreparation__Enabled` и `Agent__Voice__Enabled` при первом
создании среды остаются `false`. Их включение выполняется после deployment и smoke tests отдельным
явным параметром или шагом.

### `publish.yml`

Запускается при создании GitHub Release.

1. Restore, Release build и все тесты.
2. `dotnet publish` для `src/app/Application/Application.csproj` под Linux, framework-dependent.
3. Запись release tag и UTC build time в опубликованный `appsettings.json`.
4. Создание `${AZURE_ARTIFACT_NAME}-${VERSION}.zip`.
5. Upload ZIP в общий Azure Blob Storage с неизменяемым именем версии.
6. Test job скачивает этот же ZIP из Blob Storage.
7. OIDC login и deployment ZIP в test Web App.
8. Применение App Settings и Key Vault references.
9. Health check через APIM с проверкой версии.
10. Сохранённый job **`update-swagger-test`** импортирует OpenAPI в существующий Mini App API APIM
    только после успешного health check.
11. Дополнительная проверка agent routes выполняется отдельно и не заменяет `update-swagger-test`.

Публикация и deployment намеренно разделены Blob Storage: Test и Prod получают один и тот же бинарный
артефакт, а не результат повторной сборки.

### `deploy.yml`

Ручной deployment указанной release version в `Test` или `Prod`.

1. Проверка формата версии.
2. Download ZIP из Blob Storage.
3. OIDC login выбранного GitHub Environment.
4. Deployment в существующий Web App.
5. Применение environment-specific App Settings.
6. Health/version check.
7. Для Prod — `update-swagger-prod` после health check; для Test сохраняется имя
   `update-swagger-test`.

Workflow не пересобирает код и не меняет инфраструктуру неявно. Сначала запускается `install.yml`,
затем разворачивается выбранный артефакт.

### `delete.yml`

При удалении GitHub Release:

- проверяет наличие versioned ZIP;
- удаляет только соответствующий blob;
- удаляет Git tag, если он существует;
- не удаляет Azure-инфраструктуру, таблицы или данные.

## 5. Конфигурация приложения

Несекретные настройки передаются как GitHub Environment variables и применяются скриптом
`set-web-app-settings.sh`. В их число входят:

- Dataverse URL;
- Foundry project endpoint, deployment names и token scope;
- Table endpoint и имена таблиц;
- tenant, audience, required role и разрешённый client ID бота;
- Telegram Bot ID без токена;
- timezone, лимиты tools/history/text/audio и approval TTL;
- feature flags;
- Application Insights connection string;
- `AZURE_CLIENT_ID` UAMI API.

Секреты не должны передаваться в Bicep parameters, command output или Git:

- Telegram bot token;
- artifact Storage key, пока общий artifact storage не переведён на OIDC/RBAC;
- APIM subscription keys;
- backend certificate material;
- Dataverse deployment credentials, если OIDC ещё не внедрён.

Предпочтительный целевой вариант для runtime-секретов — Key Vault reference в App Settings. Pipeline
создаёт/обновляет ссылку и назначает UAMI право читать только требуемые secrets; значение секрета
вносится отдельно уполномоченным владельцем либо из защищённого GitHub Environment secret без вывода.

## 6. APIM

CI/CD должен обслуживать две разные поверхности.

### Mini App API

- существующий API и его authentication policy не ослабляются;
- после deployment и health check выполняется сохранённый `update-swagger-test`;
- import использует опубликованный Swagger и актуальный backend URL;
- production получает аналогичный отдельный job после ручного deployment.

### Agent API

- отдельный API для app-only трафика Telegram-бота;
- operations:
  - `POST /internal/agent/messages`;
  - `POST /internal/agent/actions/{actionId}/decision`;
- backend — Timesheet API Web App;
- APIM сохраняет Bearer token MI бота и предъявляет backend certificate;
- message operation получает согласованный `forward-request timeout="60"`;
- диагностический `/profile` не создаётся;
- policy и operations задаются декларативно либо идемпотентным скриптом и проверяются после применения.

APIM service и сертификат считаются shared dependencies. Pipeline получает их имена и завершает работу
с ошибкой, если они отсутствуют; он не создаёт параллельный APIM или новый сертификат автоматически.

## 7. Entra ID и права pipeline

Для каждой среды нужны как минимум две deployment identity с GitHub OIDC:

1. Azure deployment identity:
   - Contributor на application resource group;
   - Role Based Access Control Administrator на минимальном scope для назначения RBAC;
   - API Management Service Contributor на конкретном shared APIM;
   - права читать identity Telegram Function App.
2. Directory/Dataverse deployment identity:
   - минимальные Microsoft Graph application permissions для App Registration, service principal и
     app role assignment;
   - Dataverse Application User с ролью, позволяющей создавать/обновлять Application User API и
     назначать только требуемую security role.

Admin-consent, создание OIDC federated credentials и первоначальную выдачу прав выполняет
администратор однократным bootstrap-скриптом. После bootstrap обычные install/deploy не используют
client secrets.

## 8. Первый запуск без разрушения test-контура

До первой записи Bicep выполняется inventory существующего Test:

- resource IDs, regions, SKU и names;
- UAMI привязка и RBAC;
- Web App plan/runtime/network/TLS settings;
- Storage Account и существующие tables;
- Foundry project/deployments/quota;
- App Registration/app role/assignment;
- APIM APIs, operations, effective policies и certificate reference;
- Dataverse Application User и role;
- список App Settings без вывода secret values.

Затем шаблоны параметризуются фактическими именами. Первый `what-if` должен показать только ожидаемые
добавления/нормализацию и не должен заменять Web App, identity, Storage, Foundry или APIM API. До
проверки `what-if` apply не выполняется.

## 9. Порядок реализации

### Этап 1. Baseline и контракт параметров

- Зафиксировать test inventory и ownership matrix.
- Составить таблицу repository variables, Environment variables и secrets для Test/Prod.
- Определить, какие существующие ресурсы принимаются под IaC с теми же именами.
- Подготовить bootstrap-скрипт прав администратора.

Результат: ни одного изменения Azure; согласованный набор входов и прав.

### Этап 2. Build и release artifact

- Переписать `build.yml` по структуре эталона.
- Привести `publish-release.yml` к `publish.yml`.
- Сохранить Blob upload/download и versioned ZIP.
- Исправить `delete.yml` и проверить shell-синтаксис.

Результат: воспроизводимый артефакт, но без изменения инфраструктуры.

### Этап 3. Базовая Azure-инфраструктура

- Добавить Bicep для observability, plan, Web App, UAMI и Storage Tables.
- Добавить RBAC и App Settings без включения agent feature flags.
- Проверить Bicep build и Azure `what-if` на Test.

Результат: основной API host описан кодом и может быть идемпотентно установлен.

### Этап 4. Foundry

- Добавить AI resource/project, chat и voice deployments.
- Параметризовать region/model/version/SKU/capacity.
- Назначить UAMI роль и проверить inference readiness без бизнес-запроса.

Результат: AI-зависимости создаются и проверяются pipeline.

### Этап 5. Entra ID и Dataverse

- Добавить идемпотентную настройку agent App Registration/app role/assignment MI бота.
- Добавить настройку Dataverse Application User UAMI API.
- Не выводить токены и персональные данные в logs.

Результат: авторизация воспроизводима без ручного редактирования Portal.

### Этап 6. APIM

- Описать agent API, обе operations и policies.
- Сохранить Mini App policy.
- Сохранить `update-swagger-test` в цепочке после health check.
- Проверить negative auth matrix 401/403 и backend certificate boundary.

Результат: обе API-поверхности восстанавливаются и проверяются pipeline.

### Этап 7. Deployment workflows

- Реализовать `install.yml` и универсальный `deploy.yml` для Test/Prod.
- Test auto-deploy оставить в `publish.yml`.
- Prod deploy выполняется вручную по versioned artifact.
- Добавить health/version/APIM validation и безопасные logout/cleanup steps.

Результат: одна версия проходит путь Blob → Test → Prod без rebuild.

### Этап 8. Test rehearsal и документация

- Выполнить `what-if`, install и повторный install для проверки идемпотентности.
- Выпустить тестовый release и проверить Blob artifact.
- Пройти Mini App, text agent, voice agent и create/update/delete Cancel/Confirm smoke matrix.
- Обновить Azure change log и production rollout runbook фактическими workflow/variables/rollback.

Результат: проверенный Test CI/CD и готовый Prod runbook.

## 10. Правила выполнения

- Каждый этап реализуется отдельным инкрементом без commit до review пользователя.
- Перед изменением Azure показывается фактический `what-if` или точный перечень CLI-операций.
- Все Azure-изменения сразу фиксируются в `telegram-ai-agent-azure-change-log.md`.
- Секретные значения никогда не сохраняются в Git или документации.
- Existing resource не удаляется и не заменяется ради унификации имён.
- `Complete` deployment mode не используется.
- Agent feature flags включаются только после deployment зависимостей и smoke tests.
- `update-swagger-test` не переименовывается и не удаляется.
- Публикация через Blob Storage остаётся обязательной частью release flow.

## 11. Ближайший шаг

Начать с этапа 1: снять полный read-only inventory Test, сопоставить его с будущими Bicep resources и
подготовить таблицу GitHub variables/secrets и bootstrap-скрипт для администратора. До одобрения этого
baseline не изменять workflows и Azure.

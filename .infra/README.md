# Инфраструктура и CI/CD Timesheet API

## Назначение workflow

- `build.yml` проверяет Bicep и shell-скрипты, собирает решение и запускает тесты.
- `install.yml` создаёт или обновляет инфраструктуру выбранной среды `Test` или `Prod`,
  настраивает Timesheet-маршруты общего APIM, публикует bootstrap ZIP через Blob Storage,
  разворачивает код API, проверяет health и импортирует Swagger.
- `publish.yml` собирает GitHub Release, публикует ZIP в Blob Storage, скачивает этот же ZIP,
  разворачивает его на Test, проверяет health endpoint и выполняет `update-swagger-test`.
- `deploy.yml` скачивает из Blob Storage уже существующую версию и разворачивает её на Test или Prod
  без повторной сборки.
- `delete.yml` удаляет ZIP удалённого GitHub Release из Blob Storage и удаляет Git-тег.

Ресурсы приложения разворачиваются в режиме `Incremental`: отсутствующие ресурсы создаются,
существующие обновляются, посторонние ресурсы из Resource Group не удаляются. Общие APIM,
Dataverse, Telegram Function App, Storage Account релизов и существующий APIM-сертификат не пересоздаются.
Контейнер релизов и отсутствующий APIM-сертификат создаются при установке.

## Что должно существовать до запуска CI/CD

### App Registration для развёртывания

Для каждой среды заранее создаются две отдельные App Registration:

1. **Azure deployment App Registration** — используется GitHub Actions для создания Azure-ресурсов,
   изменения Web App и настройки APIM.
2. **Directory/Dataverse deployment App Registration** — используется для создания и настройки
   App Registration агента и регистрации Managed Identity API в Dataverse.

Для обеих App Registration должен существовать Service Principal (`Enterprise application`). Client
Secret не требуется: GitHub авторизуется через OIDC. Federated Credential создаёт приведённый ниже
bootstrap-скрипт.

App Registration `api-timesheet-agent-test`/`api-timesheet-agent-prod` заранее создавать не нужно.
Её создаёт или находит `install.yml`, после чего объявляет в ней application role
`Timesheet.Agent.Invoke` и назначает роль отдельной User Assigned MI бота для вызова Agent API.
Эту MI API создаёт вместе с общими ресурсами; Function App бота при запуске API `install.yml` не требуется.

### Общие ресурсы, которыми проект не владеет

До запуска должны существовать:

- Azure subscription приложения;
- Azure subscription интеграционной платформы с существующим APIM;
- APIM service; если backend certificate отсутствует, нужны PFX и пароль в GitHub secrets;
- Dataverse environment и роль, которую должен получить Managed Identity Timesheet API;
- Telegram-бот и его token;
- общий Storage Account для релизных артефактов; Blob Container создаёт `install.yml`;
- GitHub Environments с точными именами `Test` и `Prod`.

Resource Group приложения может отсутствовать: bootstrap-скрипт создаст её. App Service Plan, Web App,
User Assigned Managed Identity API и агента бота, Storage Tables, Application Insights, Log Analytics, Foundry
account/project и deployments моделей создаёт или обновляет `install.yml`.

### Dataverse-доступ deployment-приложения

`Directory/Dataverse deployment App Registration` нужно один раз вручную добавить в каждую среду
Dataverse как Application User. Ему назначается отдельная deployment-роль, разрешающая:

- чтение Business Unit, Security Role и Application User;
- создание и обновление Application User;
- назначение только согласованной роли Timesheet API.

После этого `install.yml` сможет идемпотентно создать или найти Application User для Managed Identity
API и назначить ему `DATAVERSE_API_ROLE_NAME`.

## Команда для администратора

Команда выполняется администратором один раз для каждой среды из корня репозитория. Перед запуском
нужно войти в нужный tenant через `az login`. Ниже указаны подтверждённые Client ID для Test.

У выполняющего команду администратора должны быть права назначать Azure RBAC-роли на Resource Group
приложения и на общий APIM, а также право выдать tenant-wide Admin Consent для Microsoft Graph.
Dataverse deployment-роль назначается отдельно администратором Dataverse, как описано выше.

Пример для Test:

```shell
az login --tenant 69879a40-68bb-4b30-941b-eb9692ddd9b4

export GITHUB_ORG='GarageGroup'
export GITHUB_REPO='internal-timesheet-service-app'
export GITHUB_ENVIRONMENT='Test'

export AZURE_DEPLOY_APP_ID='463b55ff-5b41-4eab-99d7-7f4c496bf6ab'
export DIRECTORY_DEPLOY_APP_ID='365135b9-9046-443c-941c-a84d91d093db'

export AZURE_SUBSCRIPTION_ID='73a6f94e-bfb4-4926-a9dd-09228d53a2a5'
export AZURE_RESOURCE_GROUP_NAME='rg-garage-timesheet-test'
export AZURE_LOCATION='westeurope'

export APIM_SUBSCRIPTION_ID='106dd084-8190-453f-87c9-cd2cb714b1d6'
export APIM_RESOURCE_GROUP='rg-integration-platform-test'
export APIM_SERVICE_NAME='apim-integration-platform-test-01'

set -euo pipefail

required=(
  GITHUB_ORG
  GITHUB_REPO
  GITHUB_ENVIRONMENT
  AZURE_DEPLOY_APP_ID
  DIRECTORY_DEPLOY_APP_ID
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  AZURE_LOCATION
  APIM_SUBSCRIPTION_ID
  APIM_RESOURCE_GROUP
  APIM_SERVICE_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Не задана переменная $variable" >&2; exit 1; }
done

subject="repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}"
credential_name="github-${GITHUB_REPO}-${GITHUB_ENVIRONMENT,,}"

create_federated_credential() {
  local app_id="$1"
  local existing
  local parameters

  existing="$(az ad app federated-credential list \
    --id "$app_id" \
    --query "[?name=='$credential_name'].name | [0]" \
    --output tsv \
    --only-show-errors)"

  if [[ -z "$existing" ]]; then
    parameters="$(jq -n \
      --arg name "$credential_name" \
      --arg subject "$subject" \
      '{
        name: $name,
        issuer: "https://token.actions.githubusercontent.com",
        subject: $subject,
        audiences: ["api://AzureADTokenExchange"]
      }')"

    az ad app federated-credential create \
      --id "$app_id" \
      --parameters "$parameters" \
      --output none \
      --only-show-errors
  fi
}

create_federated_credential "$AZURE_DEPLOY_APP_ID"
create_federated_credential "$DIRECTORY_DEPLOY_APP_ID"

application_scope="/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME"
apim_scope="/subscriptions/$APIM_SUBSCRIPTION_ID/resourceGroups/$APIM_RESOURCE_GROUP/providers/Microsoft.ApiManagement/service/$APIM_SERVICE_NAME"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

if [[ "$(az group exists \
  --name "$AZURE_RESOURCE_GROUP_NAME" \
  --output tsv \
  --only-show-errors)" == false ]]; then
  az group create \
    --name "$AZURE_RESOURCE_GROUP_NAME" \
    --location "$AZURE_LOCATION" \
    --tags \
      'application=garage-timesheet' \
      "environment=${GITHUB_ENVIRONMENT,,}" \
      'managedBy=github-actions' \
    --output none \
    --only-show-errors
fi

for role in 'Contributor' 'Role Based Access Control Administrator'; do
  az role assignment create \
    --assignee "$AZURE_DEPLOY_APP_ID" \
    --role "$role" \
    --scope "$application_scope" \
    --output none \
    --only-show-errors
done

az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role 'API Management Service Contributor' \
  --scope "$apim_scope" \
  --output none \
  --only-show-errors

graph_api_id='00000003-0000-0000-c000-000000000000'
application_read_write_all='1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9'
app_role_assignment_read_write_all='06b708a9-e830-4db3-a914-8e69da51d44f'
directory_read_all='7ab1d382-f21e-4acd-a863-ba3e13f7da61'

for permission in "$application_read_write_all" "$app_role_assignment_read_write_all" "$directory_read_all"; do
  az ad app permission add \
    --id "$DIRECTORY_DEPLOY_APP_ID" \
    --api "$graph_api_id" \
    --api-permissions "${permission}=Role" \
    --output none \
    --only-show-errors
done

az ad app permission admin-consent \
  --id "$DIRECTORY_DEPLOY_APP_ID" \
  --output none \
  --only-show-errors

echo 'GitHub OIDC, Azure RBAC и Microsoft Graph permissions настроены.'
```

Скрипт:

1. создаёт GitHub OIDC Federated Credential в обеих deployment App Registration;
2. создаёт Resource Group приложения, если её ещё нет;
3. выдаёт Azure deployment App Registration роли `Contributor` и
   `Role Based Access Control Administrator` на Resource Group приложения;
4. выдаёт ей `API Management Service Contributor` на используемый APIM service;
5. добавляет Directory/Dataverse deployment App Registration application permissions Microsoft Graph:
   - `Application.ReadWrite.All`;
   - `AppRoleAssignment.ReadWrite.All`;
   - `Directory.Read.All`;
6. выполняет tenant-wide Admin Consent для этих Microsoft Graph permissions.

Роль `Role Based Access Control Administrator` нужна не для повседневного доступа к данным, а для
создания Bicep role assignments: доступ Managed Identity API к Storage Tables и Azure AI Foundry.

Для Prod администратор повторяет ту же команду, заменяя:

- `GITHUB_ENVIRONMENT` на `Prod`;
- Client ID обеих production deployment App Registration;
- имена и идентификаторы Test-ресурсов на значения Prod.

## Переменные и секреты уровня репозитория

Эти значения находятся вне GitHub Environments, потому что публикация и удаление релизного артефакта
не выбирают среду.

| Имя | Тип | Назначение |
| --- | --- | --- |
| `AZURE_ARTIFACT_NAME` | Variable | Базовое имя ZIP, например `internal-timesheet-service` |
| `AZURE_ARTIFACT_ACCOUNT_NAME` | Variable | Общий Storage Account для релизов |
| `AZURE_ARTIFACT_CONTAINER_NAME` | Variable | Blob Container с релизами; создаётся при установке |
| `AZURE_ACCOUNT_KEY_ARTIFACT` | Secret | Ключ Storage Account, используемый только GitHub Actions |

## Переменные GitHub Environment

Нужно создать GitHub Environments с точными именами `Test` и `Prod`.

### Deployment identity и Azure-ресурсы

| Имя | Пример или назначение для Test |
| --- | --- |
| `DEPLOY_CLIENT_ID` | Client ID Azure deployment App Registration |
| `DIRECTORY_DEPLOY_CLIENT_ID` | Client ID Directory/Dataverse deployment App Registration |
| `DEPLOY_TENANT_ID` | `69879a40-68bb-4b30-941b-eb9692ddd9b4` |
| `DEPLOY_SUBSCRIPTION_ID` | Subscription приложения |
| `AZURE_RESOURCE_GROUP_NAME` | `rg-garage-timesheet-test` |
| `AZURE_LOCATION` | Регион создания application-ресурсов: `westeurope` для Test |
| `AZURE_NAME_POSTFIX` | `test` |
| `APP_SERVICE_PLAN_NAME` | `asp-garage-timesheet-test` |
| `APP_SERVICE_PLAN_SKU_NAME` | `B2` |
| `WEB_APP_NAME` | `app-garage-timesheet-service-test` |
| `MANAGED_IDENTITY_NAME` | `id-internal-gtimesheet-test` |
| `STORAGE_ACCOUNT_NAME` | `stinternalgtimesheettest` |
| `LOG_ANALYTICS_WORKSPACE_NAME` | `log-internal-gtimesheet-test` |
| `APPLICATION_INSIGHTS_NAME` | `appi-internal-gtimesheet-test` |

### Foundry и Storage Tables

| Имя | Пример для Test |
| --- | --- |
| `FOUNDRY_LOCATION` | Регион создания Foundry account/project: `westeurope` |
| `FOUNDRY_ACCOUNT_NAME` | `ai-timesheet-test` |
| `FOUNDRY_PROJECT_NAME` | `ai-timesheet-test` |
| `FOUNDRY_CHAT_DEPLOYMENT_NAME` | `gpt-5-mini` |
| `FOUNDRY_CHAT_MODEL_NAME` | `gpt-5-mini` |
| `FOUNDRY_CHAT_MODEL_VERSION` | `2025-08-07` |
| `FOUNDRY_CHAT_SKU_NAME` | `GlobalStandard` |
| `FOUNDRY_CHAT_CAPACITY` | `500` |
| `FOUNDRY_VOICE_DEPLOYMENT_NAME` | `whisper` |
| `FOUNDRY_VOICE_MODEL_NAME` | `whisper` |
| `FOUNDRY_VOICE_MODEL_VERSION` | `001` |
| `FOUNDRY_VOICE_SKU_NAME` | `Standard` |
| `FOUNDRY_VOICE_CAPACITY` | `3` |
| `AGENT_CONVERSATION_TABLE_NAME` | `TimesheetAgentConversation` |
| `AGENT_ACTION_TABLE_NAME` | `TimesheetAgentAction` |

До установки нужно проверить доступность моделей, квоты и требования к размещению данных в регионе.
Значения capacity из Test нельзя автоматически переносить в Prod без отдельной проверки квоты.

### Entra ID и Dataverse

| Имя | Назначение |
| --- | --- |
| `AGENT_APP_DISPLAY_NAME` | `api-timesheet-agent-test` для Test |
| `AGENT_APP_IDENTIFIER_URI` | URI существующей регистрации; для новой можно оставить пустым, будет `api://<client-id>` |
| `TELEGRAM_BOT_FUNCTION_APP_NAME` | `func-internal-gtimesheet-test`; используется для имени агентской UAMI `<имя Function App>-agent`, сама Function App пока не нужна |
| `TELEGRAM_BOT_ID` | Числовой Telegram Bot ID, не token |
| `DATAVERSE_SERVICE_URL` | URL среды без завершающего `/` |
| `DATAVERSE_API_ROLE_NAME` | Согласованная Dataverse-роль для UAMI API |

Client ID агентской UAMI записывается в настройки API: по нему API проверяет, какой клиент получил токен.
API `install.yml` всегда выполняет настройку App Registration, Dataverse и APIM: успешный запуск
не оставляет обязательные интеграции пропущенными.

### Feature flags агента

| Имя | Рекомендуемое начальное значение |
| --- | --- |
| `AGENT_ENABLED` | `true` |
| `AGENT_WRITE_PREPARATION_ENABLED` | `true` |
| `AGENT_VOICE_ENABLED` | `true` |

При создании новой среды агент включается во время установки. Если любой из трёх флагов
задан как `false`, `install.yml` завершится ошибкой: успешная установка должна включать
текстовые и голосовые запросы и подготовку операций со временем.

### APIM

| Имя | Назначение |
| --- | --- |
| `APIM_SUBSCRIPTION_ID` | Subscription общего APIM |
| `APIM_RESOURCE_GROUP` | Resource Group общего APIM |
| `APIM_SERVICE_NAME` | Имя APIM service |
| `APIM_BACKEND_CERTIFICATE_ID` | ID backend-сертификата; Test использует `timesheet-certificate` |
| `APIM_CERTIFICATE_CLIENT_ID` | Client ID разрешённого клиента основного API; обязателен, если в APIM ещё нет named value `TimesheetCertificateClientId` |
| `APIM_AGENT_API_ID` | Test использует `garage-timesheet-agent-api` |
| `APIM_AGENT_API_PATH` | Test использует `timesheet-agent` |
| `APIM_AGENT_MESSAGE_TIMEOUT_SECONDS` | `60` |
| `APIM_WEB_API_ID` | ID Mini App API; создаётся или обновляется при установке |
| `APIM_WEB_API_PATH` | URL suffix основного API; Test использует `timesheet` |
| `APIM_SWAGGER_NAME` | Сегмент Swagger route; Test использует `timesheet` |
| `APIM_HEALTH_NAME` | Сегмент health route; Test использует `timesheet` |
| `APIM_DNS` | DNS gateway выбранной среды без `https://`; Test использует `apim-integration-platform-test-01.azure-api.net` |
| `TEST_APIM_DNS` | DNS Test gateway для автоматического deployment: `apim-integration-platform-test-01.azure-api.net` |

`APIM_CERTIFICATE_CLIENT_ID` берётся из App Registration клиента, который сейчас проходит
проверку JWT в политике основного Timesheet API. Если named value уже есть, `install.yml`
использует её и проверяет совпадение с заданной переменной; чужое значение общего APIM
скрипт автоматически не перезаписывает.

### Пояснение неоднозначных переменных

#### `AZURE_LOCATION`

Основной Azure region для создания Resource Group, Managed Identity, App Service Plan, Web App,
Storage, Log Analytics и Application Insights. Для текущего Test используется `westeurope`.

Переменная задаёт место создания нового ресурса. Install-скрипт не требует, чтобы каждый уже
существующий ресурс находился именно в этом регионе, и не пытается переносить существующую Managed
Identity: её фактическая локация определяется автоматически и сохраняется.

```shell
az group show --name rg-garage-timesheet-test --query location --output tsv
```

#### `FOUNDRY_LOCATION`

Регион создания Foundry account и Foundry project. Для Test используется `westeurope`. Переменная
вынесена отдельно, потому что доступность моделей и квоты Foundry зависят от региона. Если Foundry
account уже существует, install-скрипт сохраняет его фактическую локацию и не требует её совпадения с
`FOUNDRY_LOCATION`.

RBAC assignments больше не управляются флагом: Bicep всегда назначает Managed Identity API роли
`Storage Table Data Contributor` и `Cognitive Services User`. Настройки Foundry
`versionUpgradeOption`, `raiPolicyName` и `disableLocalAuth` закреплены в Bicep соответственно как
`OnceNewDefaultVersionAvailable`, `Microsoft.DefaultV2` и `false`.

#### `APIM_DNS` и `TEST_APIM_DNS`

Обе переменные содержат только имя публичного gateway без протокола и завершающего `/`.

- `APIM_DNS` берётся из выбранного GitHub Environment и применяется в ручном `deploy.yml` для Test
  или Prod.
- `TEST_APIM_DNS` всегда указывает на Test и используется `publish.yml`, который после создания
  релиза автоматически разворачивает его на Test.

Для Test обе переменные имеют одинаковое значение: `apim-integration-platform-test-01.azure-api.net`.
Через этот gateway доступны маршруты `/health/...` и `/swagger/...` с соответствующими APIM subscription keys.

Искать значение нужно в существующих GitHub repository/environment variables старого pipeline либо в
настройках корпоративного DNS/gateway перед APIM. Стандартный Azure hostname APIM можно посмотреть в
Azure Portal: `API Management → Overview → Gateway URL`, но он может отличаться от корпоративного
публичного DNS. Для текущего APIM стандартный hostname —
`apim-integration-platform-test-01.azure-api.net`. Прежний корпоративный адрес
`api.test.garage-group.io` сейчас указывает на неразрешимое имя
`apim-integration-platform-test.azure-api.net`, поэтому Test CI/CD использует прямой hostname APIM.

### Где найти значения APIM route

#### `APIM_WEB_API_PATH`

Azure Portal: `API Management → APIs → Garage Timesheet API → Settings → URL suffix`.
Текущее Test-значение — `timesheet`. Через CLI:

```shell
az apim api show \
  --resource-group rg-integration-platform-test \
  --service-name apim-integration-platform-test-01 \
  --api-id garage-timesheet-api \
  --query path \
  --output tsv
```

#### `APIM_SWAGGER_NAME`

Это часть URL между `/swagger/` и `/swagger.json`. Её можно найти в APIM API `Swagger API`, открыв
операцию `Get timesheet service document`. Для Test операция имеет шаблон
`/timesheet/swagger.json`, поэтому значение — `timesheet`.

#### `APIM_HEALTH_NAME`

Это часть URL после `/health/`. Её можно найти в APIM API `Health Check API`, открыв операцию
`Timesheet bot api`. Для Test операция имеет шаблон `/timesheet`, поэтому значение — `timesheet`.

## Секреты GitHub Environment

| Имя | Назначение |
| --- | --- |
| `TELEGRAM_BOT_TOKEN` | Runtime token Telegram-бота; не выводится в лог и не хранится в Git |
| `APIM_BACKEND_CERTIFICATE_PFX_BASE64` | PFX backend-сертификата в Base64, если сертификата ещё нет в общем APIM |
| `APIM_BACKEND_CERTIFICATE_PASSWORD` | Пароль указанного PFX, если сертификата ещё нет в общем APIM |

Ключи `TimesheetHealthSubscription` и `TimesheetSwaggerSubscription` создаются в общем APIM
и читаются pipeline при health-проверке и импорте Swagger. В GitHub их хранить не требуется.

## Первый запуск для Test

1. Убедиться, что администратор выполнил API bootstrap-команду для Test.
2. Добавить Directory deployment App Registration API в Dataverse как Application User.
3. Заполнить repository variables/secrets и GitHub Environment `Test`.
4. Сверить имена существующих Test-ресурсов со значениями переменных.
5. Запустить API `install.yml`, выбрав `Test`: он создаст общие ресурсы, агентскую UAMI бота,
   App Registration, маршруты общего APIM и опубликует код API.
6. Запустить bot `install.yml` один раз: он создаст Function App и APIM бота, подключит UAMI,
   опубликует код бота и установит Telegram webhook.
7. Проверить пользовательский сценарий в боте. Отдельный Release для первого запуска не требуется.

Workflow сразу создаёт или обновляет ресурсы в режиме `Incremental`. Для существующего Test шаблон
предусматривает следующие нормализации конфигурации:

- создаёт новую агентскую UAMI бота и назначает ей `Timesheet.Agent.Invoke`;
- запрещает публичный доступ к Blob Storage;
- отключает FTPS и включает HTTP/2 для Web App;
- связывает Application Insights с управляемым Log Analytics Workspace.

Перед созданием CI/CD шаблоны были локально проверены через Azure `what-if`: удаления или замены Web
App, Managed Identity, Storage, Foundry account/project, model deployments и APIM API не обнаружено.

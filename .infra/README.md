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
User Assigned Managed Identity API, Storage Account, Application Insights и Log Analytics создаёт или
обновляет `install.yml`.

### Dataverse-доступ deployment-приложения

`Directory/Dataverse deployment App Registration` нужно один раз вручную добавить в каждую среду
Dataverse как Application User. Ему назначается отдельная deployment-роль, разрешающая:

- чтение Business Unit, Security Role и Application User;
- создание и обновление Application User;
- назначение только согласованной роли Timesheet API.

После этого `install.yml` сможет идемпотентно создать или найти Application User для Managed Identity
API и назначить ему `DATAVERSE_API_ROLE_NAME`.

Для существующего Application User роль выбирается в его Business Unit. Новый Application User
создаётся в корневом Business Unit с одноимённой ролью этого подразделения. Если подходящая роль
не найдена или выбор неоднозначен, install останавливается с диагностикой; существующий пользователь
не переносится в другое подразделение.

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

apim_group_scope="/subscriptions/$APIM_SUBSCRIPTION_ID/resourceGroups/$APIM_RESOURCE_GROUP"
deployment_role_name="Timesheet ARM Deployment Operator ${GITHUB_ENVIRONMENT}"
if [[ "$GITHUB_ENVIRONMENT" == Test ]]; then
  deployment_role_name='Timesheet ARM Deployment Operator'
fi
deployment_role_id="$(az role definition list \
  --name "$deployment_role_name" \
  --subscription "$APIM_SUBSCRIPTION_ID" \
  --query '[0].name' --output tsv --only-show-errors)"

if [[ -z "$deployment_role_id" ]]; then
  deployment_role_json="$(jq -n --arg name "$deployment_role_name" --arg scope "$apim_group_scope" '{
    Name: $name,
    IsCustom: true,
    Description: "Manage Timesheet ARM deployments; resource permissions are assigned separately.",
    Actions: ["Microsoft.Resources/deployments/*", "Microsoft.Resources/subscriptions/resourceGroups/read"],
    NotActions: [], DataActions: [], NotDataActions: [], AssignableScopes: [$scope]
  }')"
  deployment_role_id="$(az role definition create \
    --role-definition "$deployment_role_json" \
    --subscription "$APIM_SUBSCRIPTION_ID" \
    --query name --output tsv --only-show-errors)"
fi

azure_deploy_principal_id="$(az ad sp show --id "$AZURE_DEPLOY_APP_ID" --query id --output tsv --only-show-errors)"
deployment_assignment_count="$(az role assignment list \
  --scope "$apim_group_scope" --role "$deployment_role_id" \
  --subscription "$APIM_SUBSCRIPTION_ID" --output json --only-show-errors | \
  jq --arg scope "$apim_group_scope" --arg principal "$azure_deploy_principal_id" \
    '[.[] | select((.scope | ascii_downcase) == ($scope | ascii_downcase) and (.principalId | ascii_downcase) == ($principal | ascii_downcase))] | length')"
if [[ "$deployment_assignment_count" == 0 ]]; then
  az role assignment create \
    --assignee-object-id "$azure_deploy_principal_id" \
    --assignee-principal-type ServicePrincipal \
    --role "$deployment_role_id" --scope "$apim_group_scope" \
    --subscription "$APIM_SUBSCRIPTION_ID" --output none --only-show-errors
fi

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
5. создаёт отдельную для среды custom role `Timesheet ARM Deployment Operator` для Test
   или `Timesheet ARM Deployment Operator Prod` для Prod
   и назначает её Azure deployment App Registration на RG общего APIM; права ограничены
   ARM deployments и чтением RG, а доступ к ресурсам APIM задаётся предыдущим шагом;
6. добавляет Directory/Dataverse deployment App Registration application permissions Microsoft Graph:
   - `Application.ReadWrite.All`;
   - `AppRoleAssignment.ReadWrite.All`;
   - `Directory.Read.All`;
7. выполняет tenant-wide Admin Consent для этих Microsoft Graph permissions.

Создание custom role требует `Microsoft.Authorization/roleDefinitions/write`, назначение —
`Microsoft.Authorization/roleAssignments/write` на соответствующем scope. Повторный запуск
переиспользует роль с тем же именем и существующее назначение, включая уже созданную вручную роль Test.

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

### Entra ID и Dataverse

| Имя | Назначение |
| --- | --- |
| `DATAVERSE_SERVICE_URL` | URL среды без завершающего `/` |
| `DATAVERSE_API_ROLE_NAME` | Согласованная Dataverse-роль для UAMI API |

API `install.yml` всегда выполняет настройку Dataverse и APIM: успешный запуск не оставляет
обязательные интеграции пропущенными.

### APIM

| Имя | Назначение |
| --- | --- |
| `APIM_SUBSCRIPTION_ID` | Subscription общего APIM |
| `APIM_RESOURCE_GROUP` | Resource Group общего APIM |
| `APIM_SERVICE_NAME` | Имя APIM service |
| `APIM_BACKEND_CERTIFICATE_ID` | ID backend-сертификата; Test использует `timesheet-certificate` |
| `APIM_CERTIFICATE_CLIENT_ID` | Client ID разрешённого клиента основного API; обязателен, если в APIM ещё нет named value `TimesheetCertificateClientId` |
| `APIM_BOT_SIGN_IN_CLIENT_ID` | Client ID App Registration, через которую Telegram-бот получает токены пользователей; обязателен, если в APIM ещё нет named value `TimesheetBotSignInClientId` |
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

`APIM_BOT_SIGN_IN_CLIENT_ID` — второй клиент, которому политика основного Timesheet API разрешает
вызовы от имени пользователя: App Registration `bot-internal-gtimesheet-signin-<среда>`. Правила для
named value те же, что и для `APIM_CERTIFICATE_CLIENT_ID`.

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
5. Запустить API `install.yml`, выбрав `Test`: он создаст общие ресурсы, маршруты общего APIM
   и опубликует код API.
6. Запустить bot `install.yml` один раз: он создаст Function App, APIM бота, агентскую UAMI и Foundry,
   опубликует код бота и установит Telegram webhook.
7. Проверить пользовательский сценарий в боте. Отдельный Release для первого запуска не требуется.

Workflow сразу создаёт или обновляет ресурсы в режиме `Incremental`. Для существующего Test шаблон
предусматривает следующие нормализации конфигурации:

- запрещает публичный доступ к Blob Storage;
- отключает FTPS и включает HTTP/2 для Web App;
- связывает Application Insights с управляемым Log Analytics Workspace.

Перед созданием CI/CD шаблоны были локально проверены через Azure `what-if`: удаления или замены Web
App, Managed Identity, Storage и APIM API не обнаружено.

## Агент перенесён в Telegram-бота

AI-агент больше не входит в Timesheet API: бот сам обращается к модели и вызывает обычные операции API
от имени пользователя. Из проекта удалены endpoint `/internal/agent/*`, отдельная схема авторизации,
Foundry, таблицы истории и агентская UAMI бота. Foundry и UAMI теперь создаёт `install.yml` бота.

Развёртывание в режиме `Incremental` не удаляет ресурсы, исчезнувшие из шаблонов. После того как бот
в среде переведён на собственного агента, вручную можно удалить:

- API `garage-timesheet-agent-api` (path `timesheet-agent`) в общем APIM;
- App Registration `api-timesheet-agent-<среда>` и роль `Timesheet.Agent.Invoke`;
- таблицы `TimesheetAgentConversation` и `TimesheetAgentAction` в Storage Account;
- назначения ролей `Storage Table Data Contributor` и `Cognitive Services User` для UAMI API.

Настройки `Agent__*` из Web App удаляет сам `install.yml`. Foundry account и UAMI `<Function App>-agent`
удалять нельзя: ими пользуется бот.

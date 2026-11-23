# Единая подготовка production для API и бота

Запустить весь блок в Azure Cloud Shell **Bash**. ID и scope уже заполнены.
Нужны права управления App Registration, tenant-wide Graph consent, назначения RBAC на обеих RG
и регистрации resource providers на обеих подписках. Скрипт допускает повторный запуск.
Он не запускает install и не разворачивает код.

API deployment получает Contributor и Role Based Access Control Administrator на RG приложения
и интеграционную RG (там размещаются Foundry и APIM). Поэтому отдельная custom role для ARM
на интеграционной RG не требуется: Contributor уже включает deployment actions.
Бот получает Contributor только на RG приложения. Graph permissions выдаются только Directory app.

```shell
#!/usr/bin/env bash
set -euo pipefail
az login --tenant 69879a40-68bb-4b30-941b-eb9692ddd9b4 --output none
app_sub=106dd084-8190-453f-87c9-cd2cb714b1d6
integration_sub=12495afb-c46a-4d23-9e80-3a1dae5406ff
app_rg=rg-garage-timesheet-prod
integration_rg=rg-integration-platform-prod
api_app=2c8c71c5-a285-4769-971e-57c04a60b65c
directory_app=669c265b-2d22-47ca-ab3d-27ef56e29ed0
bot_app=087935b6-8295-4bf0-8f53-7085721151ea
app_scope="/subscriptions/$app_sub/resourceGroups/$app_rg"
integration_scope="/subscriptions/$integration_sub/resourceGroups/$integration_rg"

for command in az jq; do command -v "$command" >/dev/null || { echo "Missing $command" >&2; exit 1; }; done
az group show --name "$app_rg" --subscription "$app_sub" --output none
az group show --name "$integration_rg" --subscription "$integration_sub" --output none

ensure_sp() {
  local app="$1" id
  id="$(az ad sp list --filter "appId eq '$app'" --query '[0].id' -o tsv)"
  if [[ -z "$id" ]]; then id="$(az ad sp create --id "$app" --query id -o tsv)"; fi
  printf '%s' "$id"
}
api_principal="$(ensure_sp "$api_app")"
bot_principal="$(ensure_sp "$bot_app")"
ensure_sp "$directory_app" >/dev/null

ensure_oidc() {
  local app="$1" repo="$2" name subject existing body id
  name="github-${repo}-prod"
  subject="repo:GarageGroup/${repo}:environment:Prod"
  existing="$(az ad app federated-credential list --id "$app" -o json | jq --arg name "$name" '[.[] | select(.name == $name)]')"
  body="$(jq -n --arg name "$name" --arg subject "$subject" '{name:$name,issuer:"https://token.actions.githubusercontent.com",subject:$subject,audiences:["api://AzureADTokenExchange"]}')"
  if [[ "$(jq length <<< "$existing")" == 0 ]]; then
    az ad app federated-credential create --id "$app" --parameters "$body" --output none
  else
    id="$(jq -r '.[0].id' <<< "$existing")"
    az ad app federated-credential update --id "$app" --federated-credential-id "$id" --parameters "$body" --output none
  fi
}
ensure_oidc "$api_app" internal-timesheet-service-app
ensure_oidc "$directory_app" internal-timesheet-service-app
ensure_oidc "$bot_app" internal-timesheet-bot-app

ensure_role() {
  local principal="$1" role="$2" scope="$3" subscription="$4" count
  count="$(az role assignment list --scope "$scope" --role "$role" --subscription "$subscription" -o json | jq --arg p "$principal" --arg s "$scope" '[.[] | select((.principalId|ascii_downcase)==($p|ascii_downcase) and (.scope|ascii_downcase)==($s|ascii_downcase))] | length')"
  if [[ "$count" == 0 ]]; then
    az role assignment create --assignee-object-id "$principal" --assignee-principal-type ServicePrincipal --role "$role" --scope "$scope" --subscription "$subscription" --output none
  fi
}
# API manages application resources, Foundry and its runtime RBAC.
for scope_pair in "$app_sub|$app_scope" "$integration_sub|$integration_scope"; do
  subscription="${scope_pair%%|*}"; scope="${scope_pair#*|}"
  ensure_role "$api_principal" Contributor "$scope" "$subscription"
  ensure_role "$api_principal" 'Role Based Access Control Administrator' "$scope" "$subscription"
done
# Contributor on integration RG also covers APIM and ARM deployments.
ensure_role "$bot_principal" Contributor "$app_scope" "$app_sub"

for permission in 1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9 06b708a9-e830-4db3-a914-8e69da51d44f 7ab1d382-f21e-4acd-a863-ba3e13f7da61; do
  az ad app permission add --id "$directory_app" --api 00000003-0000-0000-c000-000000000000 --api-permissions "${permission}=Role" --output none
done
az ad app permission admin-consent --id "$directory_app"

for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.OperationalInsights Microsoft.ManagedIdentity Microsoft.CognitiveServices Microsoft.Authorization; do
  az provider register --namespace "$provider" --subscription "$app_sub" --wait --output none
done
for provider in Microsoft.CognitiveServices Microsoft.Resources Microsoft.Authorization; do
  az provider register --namespace "$provider" --subscription "$integration_sub" --wait --output none
done

echo 'Azure RBAC and GitHub OIDC for API and bot are configured. See the confirmed Dataverse prerequisite below.'
```

## Подтверждённая подготовка Dataverse

23.11.2026 read-only запросом подтверждено: Application User `# app-github-timesheet-api-prod`
для приложения `669c265b-2d22-47ca-ab3d-27ef56e29ed0` существует в production Dataverse,
активен и имеет `System Administrator`. System User ID: `24fc1167-4ae7-f111-84e9-6045bd925135`.
Это предварительное условие уже выполнено администратором; скрипт не изменяет роли Dataverse.
Runtime Managed Identity API получит `G-TimeSheetBot` через API install.

Секрет TELEGRAM_BOT_TOKEN задаётся в GitHub Prod API целиком, в этот файл не вставляется.
После подготовки выполняются API install Prod, затем bot install Prod владельцем.

Проверка 23.11.2026: shell-синтаксис; скрипт в production не запускался. Одобрено владельцем для коммита.

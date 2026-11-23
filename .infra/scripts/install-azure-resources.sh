#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  AZURE_LOCATION
  FOUNDRY_LOCATION
  ENVIRONMENT_NAME
  APP_SERVICE_PLAN_NAME
  WEB_APP_NAME
  MANAGED_IDENTITY_NAME
  TELEGRAM_BOT_FUNCTION_APP_NAME
  STORAGE_ACCOUNT_NAME
  LOG_ANALYTICS_WORKSPACE_NAME
  APPLICATION_INSIGHTS_NAME
  FOUNDRY_ACCOUNT_NAME
  FOUNDRY_PROJECT_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

[[ "$STORAGE_ACCOUNT_NAME" =~ ^[a-z0-9]{3,24}$ ]] || {
  echo 'STORAGE_ACCOUNT_NAME must contain 3-24 lowercase letters and digits' >&2
  exit 1
}

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.OperationalInsights Microsoft.ManagedIdentity Microsoft.CognitiveServices Microsoft.Authorization; do
  state="$(az provider show --namespace "$provider" --query registrationState --output tsv --only-show-errors)"
  [[ "$state" == 'Registered' ]] || { echo "Azure resource provider is not registered: $provider" >&2; exit 1; }
done

if [[ "$(az group exists --name "$AZURE_RESOURCE_GROUP_NAME" --output tsv --only-show-errors)" == false ]]; then
  az group create \
    --name "$AZURE_RESOURCE_GROUP_NAME" \
    --location "$AZURE_LOCATION" \
    --tags "application=garage-timesheet" "environment=$ENVIRONMENT_NAME" "managedBy=github-actions" \
    --output none \
    --only-show-errors
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
template="$script_dir/../main.bicep"
deployment_name="timesheet-service-${ENVIRONMENT_NAME}"

identity_location="$(az identity show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$MANAGED_IDENTITY_NAME" \
  --query location \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
identity_location="${identity_location:-$AZURE_LOCATION}"

FOUNDRY_SUBSCRIPTION_ID="${FOUNDRY_SUBSCRIPTION_ID:-$AZURE_SUBSCRIPTION_ID}"
FOUNDRY_RESOURCE_GROUP="${FOUNDRY_RESOURCE_GROUP:-$AZURE_RESOURCE_GROUP_NAME}"
foundry_provider_state="$(az provider show --namespace Microsoft.CognitiveServices --subscription "$FOUNDRY_SUBSCRIPTION_ID" --query registrationState --output tsv --only-show-errors)"
[[ "$foundry_provider_state" == Registered ]] || { echo 'Microsoft.CognitiveServices is not registered in Foundry subscription' >&2; exit 1; }
az group show --name "$FOUNDRY_RESOURCE_GROUP" --subscription "$FOUNDRY_SUBSCRIPTION_ID" --output none --only-show-errors

resource_locations='{}'
for entry in "identity:Microsoft.ManagedIdentity/userAssignedIdentities:$MANAGED_IDENTITY_NAME" "botAgentIdentity:Microsoft.ManagedIdentity/userAssignedIdentities:${TELEGRAM_BOT_FUNCTION_APP_NAME}-agent" "workspace:Microsoft.OperationalInsights/workspaces:$LOG_ANALYTICS_WORKSPACE_NAME" "insights:Microsoft.Insights/components:$APPLICATION_INSIGHTS_NAME" "storage:Microsoft.Storage/storageAccounts:$STORAGE_ACCOUNT_NAME" "plan:Microsoft.Web/serverfarms:$APP_SERVICE_PLAN_NAME" "webApp:Microsoft.Web/sites:$WEB_APP_NAME"; do
  IFS=: read -r key type name <<< "$entry"
  resources="$(az resource list --resource-group "$AZURE_RESOURCE_GROUP_NAME" --resource-type "$type" --query "[?name=='$name'].location | [0]" --output tsv --only-show-errors)"
  if [[ -n "$resources" ]]; then
    resource_locations="$(jq --arg key "$key" --arg value "$resources" '. + {($key): $value}' <<< "$resource_locations")"
  fi
done

foundry_location="$(az cognitiveservices account show \
  --resource-group "$FOUNDRY_RESOURCE_GROUP" \
  --name "$FOUNDRY_ACCOUNT_NAME" \
  --subscription "$FOUNDRY_SUBSCRIPTION_ID" \
  --query location \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
foundry_location="${foundry_location:-$FOUNDRY_LOCATION}"

parameters=(
  "resourceLocations=$resource_locations"
  "foundrySubscriptionId=$FOUNDRY_SUBSCRIPTION_ID"
  "foundryResourceGroupName=$FOUNDRY_RESOURCE_GROUP"
  "location=$AZURE_LOCATION"
  "identityLocation=$identity_location"
  "foundryLocation=$foundry_location"
  "environmentName=$ENVIRONMENT_NAME"
  "appServicePlanName=$APP_SERVICE_PLAN_NAME"
  "webAppName=$WEB_APP_NAME"
  "managedIdentityName=$MANAGED_IDENTITY_NAME"
  "botAgentIdentityName=${TELEGRAM_BOT_FUNCTION_APP_NAME}-agent"
  "storageAccountName=$STORAGE_ACCOUNT_NAME"
  "logAnalyticsWorkspaceName=$LOG_ANALYTICS_WORKSPACE_NAME"
  "applicationInsightsName=$APPLICATION_INSIGHTS_NAME"
  "foundryAccountName=$FOUNDRY_ACCOUNT_NAME"
  "foundryProjectName=$FOUNDRY_PROJECT_NAME"
  "appServicePlanSkuName=${APP_SERVICE_PLAN_SKU_NAME:-B2}"
  "conversationTableName=${AGENT_CONVERSATION_TABLE_NAME:-TimesheetAgentConversation}"
  "actionTableName=${AGENT_ACTION_TABLE_NAME:-TimesheetAgentAction}"
  "chatDeploymentName=${FOUNDRY_CHAT_DEPLOYMENT_NAME:-gpt-5-mini}"
  "chatModelName=${FOUNDRY_CHAT_MODEL_NAME:-gpt-5-mini}"
  "chatModelVersion=${FOUNDRY_CHAT_MODEL_VERSION:-2025-08-07}"
  "chatDeploymentSkuName=${FOUNDRY_CHAT_SKU_NAME:-GlobalStandard}"
  "chatDeploymentCapacity=${FOUNDRY_CHAT_CAPACITY:-500}"
  "voiceDeploymentName=${FOUNDRY_VOICE_DEPLOYMENT_NAME:-whisper}"
  "voiceModelName=${FOUNDRY_VOICE_MODEL_NAME:-whisper}"
  "voiceModelVersion=${FOUNDRY_VOICE_MODEL_VERSION:-001}"
  "voiceDeploymentSkuName=${FOUNDRY_VOICE_SKU_NAME:-Standard}"
  "voiceDeploymentCapacity=${FOUNDRY_VOICE_CAPACITY:-3}"
)

outputs="$(az deployment group create \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$deployment_name" \
  --mode Incremental \
  --template-file "$template" \
  --parameters "${parameters[@]}" \
  --query properties.outputs \
  --output json \
  --only-show-errors)"

principal_id="$(jq -r '.managedIdentityPrincipalId.value' <<< "$outputs")"
[[ -n "$principal_id" && "$principal_id" != null ]] || {
  echo 'Managed Identity principal ID was not returned by the deployment' >&2
  exit 1
}

ensure_role_assignment() {
  local scope="$1"
  local role_id="$2"
  local role_subscription="$3"
  local existing

  existing="$(az role assignment list \
    --subscription "$role_subscription" \
    --scope "$scope" \
    --role "$role_id" \
    --output json \
    --only-show-errors | jq -r --arg scope "$scope" --arg principal "$principal_id" \
      '[.[] | select((.scope | ascii_downcase) == ($scope | ascii_downcase) and (.principalId | ascii_downcase) == ($principal | ascii_downcase))] | length')"

  if [[ "$existing" == 0 ]]; then
    az role assignment create \
      --subscription "$role_subscription" \
      --assignee-object-id "$principal_id" \
      --assignee-principal-type ServicePrincipal \
      --role "$role_id" \
      --scope "$scope" \
      --output none \
      --only-show-errors
  fi
}

resource_group_scope="/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME"
ensure_role_assignment \
  "$resource_group_scope/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME" \
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' \
  "$AZURE_SUBSCRIPTION_ID"
ensure_role_assignment \
  "/subscriptions/$FOUNDRY_SUBSCRIPTION_ID/resourceGroups/$FOUNDRY_RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$FOUNDRY_ACCOUNT_NAME" \
  'a97b65f3-24c7-4388-baec-2e87135dc908' \
  "$FOUNDRY_SUBSCRIPTION_ID"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  jq -r '
    "web_app_name=" + .webAppName.value,
    "web_app_host_name=" + .webAppHostName.value,
    "managed_identity_client_id=" + .managedIdentityClientId.value,
    "managed_identity_principal_id=" + .managedIdentityPrincipalId.value,
    "bot_agent_identity_principal_id=" + .botAgentIdentityPrincipalId.value,
    "bot_agent_identity_client_id=" + .botAgentIdentityClientId.value,
    "storage_table_endpoint=" + .storageTableEndpoint.value,
    "foundry_project_endpoint=" + .foundryProjectEndpoint.value,
    "foundry_openai_endpoint=" + .foundryOpenAiEndpoint.value,
    "application_insights_connection_string=" + .applicationInsightsConnectionString.value
  ' <<< "$outputs" >> "$GITHUB_OUTPUT"
fi

echo "Infrastructure deployment $deployment_name completed."

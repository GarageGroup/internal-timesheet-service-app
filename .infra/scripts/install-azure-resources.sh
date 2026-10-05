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

foundry_location="$(az cognitiveservices account show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FOUNDRY_ACCOUNT_NAME" \
  --query location \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
foundry_location="${foundry_location:-$FOUNDRY_LOCATION}"

parameters=(
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

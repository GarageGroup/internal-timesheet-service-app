#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  AZURE_LOCATION
  ENVIRONMENT_NAME
  APP_SERVICE_PLAN_NAME
  WEB_APP_NAME
  MANAGED_IDENTITY_NAME
  STORAGE_ACCOUNT_NAME
  LOG_ANALYTICS_WORKSPACE_NAME
  APPLICATION_INSIGHTS_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

[[ "$STORAGE_ACCOUNT_NAME" =~ ^[a-z0-9]{3,24}$ ]] || {
  echo 'STORAGE_ACCOUNT_NAME must contain 3-24 lowercase letters and digits' >&2
  exit 1
}

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.OperationalInsights Microsoft.ManagedIdentity; do
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

resource_locations='{}'
for entry in "identity:Microsoft.ManagedIdentity/userAssignedIdentities:$MANAGED_IDENTITY_NAME" "workspace:Microsoft.OperationalInsights/workspaces:$LOG_ANALYTICS_WORKSPACE_NAME" "insights:Microsoft.Insights/components:$APPLICATION_INSIGHTS_NAME" "storage:Microsoft.Storage/storageAccounts:$STORAGE_ACCOUNT_NAME" "plan:Microsoft.Web/serverfarms:$APP_SERVICE_PLAN_NAME" "webApp:Microsoft.Web/sites:$WEB_APP_NAME"; do
  IFS=: read -r key type name <<< "$entry"
  resources="$(az resource list --resource-group "$AZURE_RESOURCE_GROUP_NAME" --resource-type "$type" --query "[?name=='$name'].location | [0]" --output tsv --only-show-errors)"
  if [[ -n "$resources" ]]; then
    resource_locations="$(jq --arg key "$key" --arg value "$resources" '. + {($key): $value}' <<< "$resource_locations")"
  fi
done

parameters=(
  "resourceLocations=$resource_locations"
  "location=$AZURE_LOCATION"
  "identityLocation=$identity_location"
  "environmentName=$ENVIRONMENT_NAME"
  "appServicePlanName=$APP_SERVICE_PLAN_NAME"
  "webAppName=$WEB_APP_NAME"
  "managedIdentityName=$MANAGED_IDENTITY_NAME"
  "storageAccountName=$STORAGE_ACCOUNT_NAME"
  "logAnalyticsWorkspaceName=$LOG_ANALYTICS_WORKSPACE_NAME"
  "applicationInsightsName=$APPLICATION_INSIGHTS_NAME"
  "appServicePlanSkuName=${APP_SERVICE_PLAN_SKU_NAME:-B2}"
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
    "application_insights_connection_string=" + .applicationInsightsConnectionString.value
  ' <<< "$outputs" >> "$GITHUB_OUTPUT"
fi

echo "Infrastructure deployment $deployment_name completed."

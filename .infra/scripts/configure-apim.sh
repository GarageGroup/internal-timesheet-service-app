#!/usr/bin/env bash
set -euo pipefail

required=(
  APIM_SUBSCRIPTION_ID
  APIM_RESOURCE_GROUP
  APIM_SERVICE_NAME
  APIM_AGENT_API_ID
  APIM_AGENT_API_PATH
  APIM_WEB_API_ID
  APIM_WEB_API_PATH
  APIM_HEALTH_NAME
  APIM_SWAGGER_NAME
  AGENT_TENANT_ID
  APIM_BACKEND_CERTIFICATE_ID
  WEB_APP_NAME
)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$APIM_SUBSCRIPTION_ID"
az apim show --resource-group "$APIM_RESOURCE_GROUP" --name "$APIM_SERVICE_NAME" --output none --only-show-errors

certificate_url="https://management.azure.com/subscriptions/$APIM_SUBSCRIPTION_ID/resourceGroups/$APIM_RESOURCE_GROUP/providers/Microsoft.ApiManagement/service/$APIM_SERVICE_NAME/certificates/$APIM_BACKEND_CERTIFICATE_ID?api-version=2024-05-01"
certificate_exists="$(az rest \
  --method GET \
  --url "$certificate_url" \
  --query id \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
if [[ -z "$certificate_exists" ]]; then
  [[ -n "${APIM_BACKEND_CERTIFICATE_PFX_BASE64:-}" && -n "${APIM_BACKEND_CERTIFICATE_PASSWORD:-}" ]] || {
    echo "Missing APIM certificate '$APIM_BACKEND_CERTIFICATE_ID' and its PFX secrets" >&2
    exit 1
  }
  certificate_body="$(jq -n --arg data "$APIM_BACKEND_CERTIFICATE_PFX_BASE64" --arg password "$APIM_BACKEND_CERTIFICATE_PASSWORD" \
    '{properties: {data: $data, password: $password}}')"
  az rest --method PUT --url "$certificate_url" --headers 'Content-Type=application/json' --body "$certificate_body" --output none --only-show-errors
fi

apim_base="https://management.azure.com/subscriptions/$APIM_SUBSCRIPTION_ID/resourceGroups/$APIM_RESOURCE_GROUP/providers/Microsoft.ApiManagement/service/$APIM_SERVICE_NAME"
ensure_named_value() {
  local name="$1"
  local expected="$2"
  local url="$apim_base/namedValues/$name?api-version=2024-05-01"
  local existing
  existing="$(az rest --method GET --url "$url" --query name --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$existing" ]]; then
    if [[ -n "$expected" ]]; then
      local actual
      actual="$(az rest --method POST --url "$apim_base/namedValues/$name/listValue?api-version=2024-05-01" --query value --output tsv --only-show-errors)"
      [[ "$actual" == "$expected" ]] || { echo "APIM named value '$name' differs from the configured value" >&2; exit 1; }
    fi
    return
  fi
  [[ -n "$expected" ]] || { echo "Missing APIM named value '$name' and its configuration" >&2; exit 1; }
  local body
  body="$(jq -n --arg name "$name" --arg value "$expected" '{properties: {displayName: $name, secret: false, value: $value}}')"
  az rest --method PUT --url "$url" --headers 'Content-Type=application/json' --body "$body" --output none --only-show-errors
}

ensure_named_value GarageTenantId "$AGENT_TENANT_ID"
ensure_named_value TimesheetCertificateClientId "${APIM_CERTIFICATE_CLIENT_ID:-}"

for api in health-check-api swagger-api; do
  path=swagger
  if [[ "$api" == health-check-api ]]; then
    path=health
  fi
  existing_path="$(az apim api show --resource-group "$APIM_RESOURCE_GROUP" --service-name "$APIM_SERVICE_NAME" --api-id "$api" --query path --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$existing_path" ]]; then
    [[ "$existing_path" == "$path" ]] || { echo "Shared APIM API '$api' has unexpected path '$existing_path'" >&2; exit 1; }
    continue
  fi
  az apim api create \
    --resource-group "$APIM_RESOURCE_GROUP" \
    --service-name "$APIM_SERVICE_NAME" \
    --api-id "$api" \
    --display-name "$api" \
    --path "$path" \
    --protocols https \
    --subscription-required true \
    --output none \
    --only-show-errors
done

for subscription in 'TimesheetHealthSubscription:health-check-api' 'TimesheetSwaggerSubscription:swagger-api'; do
  subscription_name="${subscription%%:*}"
  api_name="${subscription#*:}"
  url="$apim_base/subscriptions/$subscription_name?api-version=2024-05-01"
  existing="$(az rest --method GET --url "$url" --query name --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$existing" ]]; then
    continue
  fi
  body="$(jq -n --arg name "$subscription_name" --arg scope "$apim_base/apis/$api_name" \
    '{properties: {displayName: $name, scope: $scope, state: "active"}}')"
  az rest --method PUT --url "$url" --headers 'Content-Type=application/json' --body "$body" --output none --only-show-errors
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
az deployment group create \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --name "timesheet-agent-api-${ENVIRONMENT_NAME:-environment}" \
  --mode Incremental \
  --template-file "$script_dir/../apim/main.bicep" \
  --parameters \
    apimServiceName="$APIM_SERVICE_NAME" \
    agentApiId="$APIM_AGENT_API_ID" \
    agentApiPath="$APIM_AGENT_API_PATH" \
    backendServiceUrl="https://${WEB_APP_NAME}.azurewebsites.net/" \
    backendCertificateId="$APIM_BACKEND_CERTIFICATE_ID" \
    messageTimeoutSeconds="${APIM_AGENT_MESSAGE_TIMEOUT_SECONDS:-60}" \
    webApiId="$APIM_WEB_API_ID" \
    webApiPath="$APIM_WEB_API_PATH" \
    healthName="$APIM_HEALTH_NAME" \
    swaggerName="$APIM_SWAGGER_NAME" \
  --output none \
  --only-show-errors

echo "APIM agent API '$APIM_AGENT_API_ID' configured."

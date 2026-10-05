#!/usr/bin/env bash
set -euo pipefail

required=(APIM_RESOURCE_GROUP APIM_SERVICE_NAME APIM_WEB_API_ID APIM_WEB_API_PATH APIM_SWAGGER_NAME APIM_DNS APIM_SUBKEY WEB_APP_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az apim api import \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --service-name "$APIM_SERVICE_NAME" \
  --api-id "$APIM_WEB_API_ID" \
  --path "$APIM_WEB_API_PATH" \
  --service-url "https://${WEB_APP_NAME}.azurewebsites.net/" \
  --subscription-required false \
  --specification-format OpenApi \
  --specification-url "https://${APIM_DNS}/swagger/${APIM_SWAGGER_NAME}/swagger.json?subscription-key=${APIM_SUBKEY}" \
  --only-show-errors

echo "Swagger imported into APIM API '$APIM_WEB_API_ID'."

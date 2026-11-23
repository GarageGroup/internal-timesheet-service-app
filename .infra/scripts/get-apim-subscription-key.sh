#!/usr/bin/env bash
set -euo pipefail

required=(APIM_SUBSCRIPTION_ID APIM_RESOURCE_GROUP APIM_SERVICE_NAME APIM_SUBSCRIPTION_NAME GITHUB_OUTPUT)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$APIM_SUBSCRIPTION_ID"
url="https://management.azure.com/subscriptions/$APIM_SUBSCRIPTION_ID/resourceGroups/$APIM_RESOURCE_GROUP/providers/Microsoft.ApiManagement/service/$APIM_SERVICE_NAME/subscriptions/$APIM_SUBSCRIPTION_NAME/listSecrets?api-version=2024-05-01"
key="$(az rest --method POST --url "$url" --query primaryKey --output tsv --only-show-errors)"
[[ -n "$key" ]] || { echo "APIM subscription key '$APIM_SUBSCRIPTION_NAME' was not found" >&2; exit 1; }
echo "::add-mask::$key"
echo "key=$key" >> "$GITHUB_OUTPUT"

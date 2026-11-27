#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP_NAME WEB_APP_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

state="$(az webapp show --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$WEB_APP_NAME" --query state --output tsv --only-show-errors)"
[[ "$state" == 'Running' ]] || { echo "Web App is not running: $state" >&2; exit 1; }

echo 'Azure deployment validation completed.'

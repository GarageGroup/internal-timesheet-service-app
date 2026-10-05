#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP_NAME WEB_APP_NAME STORAGE_ACCOUNT_NAME FOUNDRY_ACCOUNT_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

state="$(az webapp show --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$WEB_APP_NAME" --query state --output tsv --only-show-errors)"
[[ "$state" == 'Running' ]] || { echo "Web App is not running: $state" >&2; exit 1; }

for table_name in "${AGENT_CONVERSATION_TABLE_NAME:-TimesheetAgentConversation}" "${AGENT_ACTION_TABLE_NAME:-TimesheetAgentAction}"; do
  table_id="/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME/tableServices/default/tables/$table_name"
  az resource show --ids "$table_id" --api-version 2023-05-01 --output none --only-show-errors
done

for deployment_name in "${FOUNDRY_CHAT_DEPLOYMENT_NAME:-gpt-5-mini}" "${FOUNDRY_VOICE_DEPLOYMENT_NAME:-whisper}"; do
  provisioning_state="$(az cognitiveservices account deployment show \
    --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
    --name "$FOUNDRY_ACCOUNT_NAME" \
    --deployment-name "$deployment_name" \
    --query properties.provisioningState \
    --output tsv \
    --only-show-errors)"
  [[ "$provisioning_state" == 'Succeeded' ]] || { echo "Foundry deployment is not ready: $deployment_name" >&2; exit 1; }
done

echo 'Azure deployment validation completed.'

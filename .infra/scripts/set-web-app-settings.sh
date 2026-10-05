#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  WEB_APP_NAME
  API_MANAGED_IDENTITY_CLIENT_ID
  APPLICATION_INSIGHTS_CONNECTION_STRING
  DATAVERSE_SERVICE_URL
  AGENT_TENANT_ID
  AGENT_AUDIENCE
  AGENT_CLIENT_ID
  TELEGRAM_BOT_TOKEN
  TELEGRAM_BOT_ID
  FOUNDRY_PROJECT_ENDPOINT
  FOUNDRY_OPENAI_ENDPOINT
  FOUNDRY_CHAT_DEPLOYMENT_NAME
  FOUNDRY_VOICE_DEPLOYMENT_NAME
  STORAGE_TABLE_ENDPOINT
)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

for variable in AGENT_ENABLED AGENT_WRITE_PREPARATION_ENABLED AGENT_VOICE_ENABLED; do
  [[ "${!variable:-true}" == true ]] || { echo "$variable must be true for a complete installation" >&2; exit 1; }
done

settings=(
  "Info__DeployDateTime=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  "AZURE_CLIENT_ID=$API_MANAGED_IDENTITY_CLIENT_ID"
  "APPLICATIONINSIGHTS_CONNECTION_STRING=$APPLICATION_INSIGHTS_CONNECTION_STRING"
  "Dataverse__ServiceUrl=${DATAVERSE_SERVICE_URL%/}"
  "TelegramBot__WebAppDataMaxAgeMinutes=${TELEGRAM_WEB_APP_DATA_MAX_AGE_MINUTES:-5}"
  "TelegramBot__WebAppDataClockSkewSeconds=${TELEGRAM_WEB_APP_DATA_CLOCK_SKEW_SECONDS:-30}"
  "Agent__Enabled=${AGENT_ENABLED:-true}"
  "Agent__Authentication__TenantId=$AGENT_TENANT_ID"
  "Agent__Authentication__Audience=$AGENT_AUDIENCE"
  "Agent__Authentication__RequiredRole=${AGENT_REQUIRED_ROLE:-Timesheet.Agent.Invoke}"
  "Agent__Authentication__Clients__0__ClientId=$AGENT_CLIENT_ID"
  "Agent__Authentication__Clients__0__BotId=$TELEGRAM_BOT_ID"
  "Agent__Foundry__ProjectEndpoint=$FOUNDRY_PROJECT_ENDPOINT"
  "Agent__Foundry__ModelId=$FOUNDRY_CHAT_DEPLOYMENT_NAME"
  "Agent__Foundry__TokenScope=https://ai.azure.com/.default"
  "Agent__Voice__Enabled=${AGENT_VOICE_ENABLED:-true}"
  "Agent__Voice__Endpoint=$FOUNDRY_OPENAI_ENDPOINT"
  "Agent__Voice__DeploymentName=$FOUNDRY_VOICE_DEPLOYMENT_NAME"
  "Agent__Voice__ModelId=${FOUNDRY_VOICE_MODEL_NAME:-whisper}"
  "Agent__Voice__MaxFileSizeBytes=${AGENT_VOICE_MAX_FILE_SIZE_BYTES:-5242880}"
  "Agent__Storage__TableServiceEndpoint=$STORAGE_TABLE_ENDPOINT"
  "Agent__Storage__ConversationTableName=${AGENT_CONVERSATION_TABLE_NAME:-TimesheetAgentConversation}"
  "Agent__Storage__ActionTableName=${AGENT_ACTION_TABLE_NAME:-TimesheetAgentAction}"
  "Agent__WritePreparation__Enabled=${AGENT_WRITE_PREPARATION_ENABLED:-true}"
  "Agent__WritePreparation__ApprovalTtlMinutes=${AGENT_APPROVAL_TTL_MINUTES:-10}"
  "Agent__Message__TimeZoneId=${AGENT_TIME_ZONE_ID:-Europe/Moscow}"
  "Agent__Message__MaxTextLength=${AGENT_MAX_TEXT_LENGTH:-2000}"
  "Agent__Message__MaxHistoryMessageCount=${AGENT_MAX_HISTORY_MESSAGE_COUNT:-20}"
  "Agent__Tools__Timesheet__MaxDateRangeInDays=${AGENT_TIMESHEET_MAX_DATE_RANGE_DAYS:-31}"
  "Agent__Tools__Project__DefaultTop=${AGENT_PROJECT_DEFAULT_TOP:-10}"
  "Agent__Tools__Project__MaxTop=${AGENT_PROJECT_MAX_TOP:-20}"
  "Agent__Tools__Project__MaxSearchTextLength=${AGENT_PROJECT_MAX_SEARCH_TEXT_LENGTH:-100}"
  "Agent__Tools__Tag__MaxTags=${AGENT_TAG_MAX_TAGS:-20}"
  "Project__LastDaysPeriod=${PROJECT_LAST_DAYS_PERIOD:-30}"
  "Project__TagsDaysPeriod=${PROJECT_TAGS_DAYS_PERIOD:-45}"
)

settings+=("TelegramBot__Token=$TELEGRAM_BOT_TOKEN")

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
az webapp config appsettings set \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$WEB_APP_NAME" \
  --settings "${settings[@]}" \
  --output none \
  --only-show-errors

echo "Web App settings updated for $WEB_APP_NAME."

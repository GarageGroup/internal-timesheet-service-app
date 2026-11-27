#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  WEB_APP_NAME
  API_MANAGED_IDENTITY_CLIENT_ID
  APPLICATION_INSIGHTS_CONNECTION_STRING
  DATAVERSE_SERVICE_URL
  TELEGRAM_BOT_TOKEN
)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

settings=(
  "Info__DeployDateTime=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  "AZURE_CLIENT_ID=$API_MANAGED_IDENTITY_CLIENT_ID"
  "APPLICATIONINSIGHTS_CONNECTION_STRING=$APPLICATION_INSIGHTS_CONNECTION_STRING"
  "Dataverse__ServiceUrl=${DATAVERSE_SERVICE_URL%/}"
  "TelegramBot__WebAppDataMaxAgeMinutes=${TELEGRAM_WEB_APP_DATA_MAX_AGE_MINUTES:-5}"
  "TelegramBot__WebAppDataClockSkewSeconds=${TELEGRAM_WEB_APP_DATA_CLOCK_SKEW_SECONDS:-30}"
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

# The agent has moved to the Telegram bot, so its settings are removed from the Web App.
obsolete_settings="$(az webapp config appsettings list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$WEB_APP_NAME" \
  --query "[?starts_with(name, 'Agent__')].name" \
  --output tsv \
  --only-show-errors)"
if [[ -n "$obsolete_settings" ]]; then
  # shellcheck disable=SC2086
  az webapp config appsettings delete \
    --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --setting-names $obsolete_settings \
    --output none \
    --only-show-errors
fi

echo "Web App settings updated for $WEB_APP_NAME."

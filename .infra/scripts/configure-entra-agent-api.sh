#!/usr/bin/env bash
set -euo pipefail

required=(
  AGENT_APP_DISPLAY_NAME
  AGENT_REQUIRED_ROLE
  BOT_AGENT_MANAGED_IDENTITY_PRINCIPAL_ID
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

apps_json="$(az ad app list --display-name "$AGENT_APP_DISPLAY_NAME" --output json --only-show-errors)"
app_count="$(jq length <<< "$apps_json")"
[[ "$app_count" -le 1 ]] || { echo "Multiple App Registrations found with display name '$AGENT_APP_DISPLAY_NAME'" >&2; exit 1; }
app_json="$(jq '.[0] // null' <<< "$apps_json")"
if [[ "$app_count" == 0 ]]; then
  app_json="$(az ad app create --display-name "$AGENT_APP_DISPLAY_NAME" --sign-in-audience AzureADMyOrg --output json --only-show-errors)"
fi

app_id="$(jq -er '.appId' <<< "$app_json")"
app_object_id="$(jq -er '.id' <<< "$app_json")"
identifier_uri="${AGENT_APP_IDENTIFIER_URI:-api://$app_id}"

current_app="$(az rest --method GET --url "https://graph.microsoft.com/v1.0/applications/$app_object_id" --output json --only-show-errors)"
role_id="$(jq -r --arg value "$AGENT_REQUIRED_ROLE" '.appRoles[]? | select(.value == $value) | .id' <<< "$current_app" | head -n 1)"
if [[ -z "$role_id" ]]; then
  role_id="$(cat /proc/sys/kernel/random/uuid)"
fi

updated_roles="$(jq -c --arg value "$AGENT_REQUIRED_ROLE" --arg id "$role_id" '
  [(.appRoles // [])[] | select(.value != $value) | {
    allowedMemberTypes,
    description,
    displayName,
    id,
    isEnabled,
    value
  }] + [{
    allowedMemberTypes: ["Application"],
    description: "Invoke Timesheet Agent API",
    displayName: $value,
    id: $id,
    isEnabled: true,
    value: $value
  }]
' <<< "$current_app")"

patch_body="$(jq -n \
  --arg uri "$identifier_uri" \
  --argjson roles "$updated_roles" \
  '{identifierUris: [$uri], appRoles: $roles}')"

az rest \
  --method PATCH \
  --url "https://graph.microsoft.com/v1.0/applications/$app_object_id" \
  --headers 'Content-Type=application/json' \
  --body "$patch_body" \
  --output none \
  --only-show-errors

agent_sp="$(az ad sp list --filter "appId eq '$app_id'" --query '[0]' --output json --only-show-errors)"
if [[ "$agent_sp" == 'null' ]]; then
  agent_sp="$(az ad sp create --id "$app_id" --output json --only-show-errors)"
fi
agent_sp_object_id="$(jq -er '.id' <<< "$agent_sp")"

for _ in {1..12}; do
  propagated_role="$(az rest \
    --method GET \
    --url "https://graph.microsoft.com/v1.0/servicePrincipals/$agent_sp_object_id?\$select=appRoles" \
    --query "appRoles[?id=='$role_id'].id | [0]" \
    --output tsv \
    --only-show-errors)"
  [[ -n "$propagated_role" ]] && break
  sleep 5
done
[[ -n "${propagated_role:-}" ]] || { echo 'Agent app role did not propagate to its service principal' >&2; exit 1; }

bot_service_principal=''
for _ in {1..12}; do
  bot_service_principal="$(az rest \
    --method GET \
    --url "https://graph.microsoft.com/v1.0/servicePrincipals/$BOT_AGENT_MANAGED_IDENTITY_PRINCIPAL_ID?\$select=id,appId" \
    --output json \
    --only-show-errors 2>/dev/null || true)"
  [[ -n "$bot_service_principal" ]] && break
  sleep 5
done
[[ -n "$bot_service_principal" ]] || { echo 'Bot agent Managed Identity did not propagate to Microsoft Graph' >&2; exit 1; }
bot_client_id="$(jq -er '.appId' <<< "$bot_service_principal")"

existing_assignment="$(az rest \
  --method GET \
  --url "https://graph.microsoft.com/v1.0/servicePrincipals/$BOT_AGENT_MANAGED_IDENTITY_PRINCIPAL_ID/appRoleAssignments" \
  --query "value[?resourceId=='$agent_sp_object_id' && appRoleId=='$role_id'] | [0].id" \
  --output tsv \
  --only-show-errors)"

if [[ -z "$existing_assignment" ]]; then
  assignment_body="$(jq -n \
    --arg principalId "$BOT_AGENT_MANAGED_IDENTITY_PRINCIPAL_ID" \
    --arg resourceId "$agent_sp_object_id" \
    --arg appRoleId "$role_id" \
    '{principalId: $principalId, resourceId: $resourceId, appRoleId: $appRoleId}')"

  az rest \
    --method POST \
    --url "https://graph.microsoft.com/v1.0/servicePrincipals/$BOT_AGENT_MANAGED_IDENTITY_PRINCIPAL_ID/appRoleAssignments" \
    --headers 'Content-Type=application/json' \
    --body "$assignment_body" \
    --output none \
    --only-show-errors
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'app_client_id=%s\napp_object_id=%s\nservice_principal_object_id=%s\nrole_id=%s\naudience=%s\ncaller_client_id=%s\n' \
    "$app_id" "$app_object_id" "$agent_sp_object_id" "$role_id" "$identifier_uri" "$bot_client_id" >> "$GITHUB_OUTPUT"
fi

echo "Agent App Registration '$AGENT_APP_DISPLAY_NAME' configured."

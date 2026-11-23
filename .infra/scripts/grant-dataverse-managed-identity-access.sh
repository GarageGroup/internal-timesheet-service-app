#!/usr/bin/env bash
set -euo pipefail

required=(DATAVERSE_SERVICE_URL API_MANAGED_IDENTITY_PRINCIPAL_ID DATAVERSE_API_ROLE_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

url="${DATAVERSE_SERVICE_URL%/}"
client_id="$(az ad sp show --id "$API_MANAGED_IDENTITY_PRINCIPAL_ID" --query appId --output tsv --only-show-errors)"
[[ -n "$client_id" ]] || { echo 'Cannot resolve API managed identity client ID' >&2; exit 1; }

token="$(az account get-access-token --resource "$url" --query accessToken --output tsv --only-show-errors)"
api="$url/api/data/v9.2"
headers=(-H "Authorization: Bearer $token" -H 'Accept: application/json' -H 'Content-Type: application/json' -H 'OData-Version: 4.0')

encoded_role_name="$(jq -rn --arg value "$DATAVERSE_API_ROLE_NAME" '$value|@uri')"
role_query="roles?%24select=roleid,name,_businessunitid_value&%24filter=name%20eq%20%27${encoded_role_name}%27"
role="$(curl --fail-with-body --silent --show-error "${headers[@]}" "$api/$role_query" | jq -er '.value[0]')"
role_id="$(jq -er '.roleid' <<< "$role")"
business_unit_id="$(jq -er '._businessunitid_value' <<< "$role")"

query="systemusers?%24select=systemuserid,_businessunitid_value,isdisabled&%24filter=applicationid%20eq%20${client_id}"
users="$(curl --fail-with-body --silent --show-error "${headers[@]}" "$api/$query" | jq '.value')"
if [[ "$(jq length <<< "$users")" == 0 ]]; then
  curl --fail-with-body --silent --show-error -X POST "${headers[@]}" "$api/systemusers" \
    --data "{\"applicationid\":\"$client_id\",\"azureactivedirectoryobjectid\":\"$API_MANAGED_IDENTITY_PRINCIPAL_ID\",\"businessunitid@odata.bind\":\"/businessunits($business_unit_id)\"}" >/dev/null
  for _ in {1..12}; do
    sleep 5
    users="$(curl --fail-with-body --silent --show-error "${headers[@]}" "$api/$query" | jq '.value')"
    [[ "$(jq length <<< "$users")" == 1 ]] && break
  done
fi

[[ "$(jq length <<< "$users")" == 1 ]] || { echo 'Dataverse Application User was not created or is duplicated' >&2; exit 1; }
user_id="$(jq -r '.[0].systemuserid' <<< "$users")"
[[ "$(jq -r '.[0].isdisabled' <<< "$users")" == false ]] || { echo 'Dataverse Application User is disabled' >&2; exit 1; }
[[ "$(jq -r '.[0]._businessunitid_value' <<< "$users" | tr '[:upper:]' '[:lower:]')" == "${business_unit_id,,}" ]] || {
  echo "Dataverse Application User belongs to a different Business Unit than role '$DATAVERSE_API_ROLE_NAME'" >&2
  exit 1
}

assigned_roles="$(curl --fail-with-body --silent --show-error "${headers[@]}" "$api/systemusers($user_id)/systemuserroles_association?%24select=roleid")"
role_is_assigned="$(jq -r --arg role "$role_id" '[.value[] | select(.roleid == $role)] | length' <<< "$assigned_roles")"
if [[ "$role_is_assigned" == 0 ]]; then
  curl --fail-with-body --silent --show-error -X POST "${headers[@]}" "$api/systemusers($user_id)/systemuserroles_association/\$ref" \
    --data "{\"@odata.id\":\"$api/roles($role_id)\"}" >/dev/null
fi

echo "Dataverse role '$DATAVERSE_API_ROLE_NAME' assigned to API managed identity."

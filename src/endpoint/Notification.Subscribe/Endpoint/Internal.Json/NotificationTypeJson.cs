using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal readonly record struct NotificationTypeJson
{
    private const string EntityPluralName = "gg_bot_notification_types";

    private const string IdFieldName = "gg_bot_notification_typeid";
    
    private const string KeyFieldName = "gg_key";

    public static DataverseEntityGetIn BuildGetInput(string typeKey)
        => new(
            entityPluralName: EntityPluralName,
            selectFields: [IdFieldName],
            entityKey: new DataverseAlternateKey(KeyFieldName, $"'{typeKey}'"));
    
    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }
}
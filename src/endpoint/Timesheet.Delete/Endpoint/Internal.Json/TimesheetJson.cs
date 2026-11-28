using GarageGroup.Infra;
using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    private const string EntityPluralName
        =
        "gg_timesheetactivities";

    private const string DateFieldName
        =
        "gg_date";

    internal static DataverseEntityGetIn BuildDataverseGetInput(Guid timesheetId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId),
            selectFields: [DateFieldName])
        {
            CallerObjectId = callerObjectId
        };

    internal static DataverseEntityDeleteIn BuildDataverseDeleteInput(Guid timesheetId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId))
        {
            CallerObjectId = callerObjectId
        };

    [JsonPropertyName(DateFieldName)]
    public DateOnly? Date { get; init; }
}
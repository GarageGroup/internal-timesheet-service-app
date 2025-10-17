using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    private const string EntityPluralName
        =
        "gg_timesheetactivities";

    internal static DataverseEntityCreateIn<TimesheetJson> BuildDataverseCreateInput(TimesheetJson timesheet, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityData: timesheet)
        {
            CallerObjectId = callerObjectId
        };

    internal static DataverseEntityUpdateIn<TimesheetJson> BuildDataverseUpdateInput(Guid timesheetId, TimesheetJson timesheet)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId),
            entityData: timesheet);

    internal TimesheetJson(IProjectJson? project = null)
    {
        if (project is null)
        {
            return;
        }

        Subject = project.Name;
        ExtensionData = new()
        {
            [$"regardingobjectid_{project.LookupEntity}@odata.bind"] = project.LookupValue
        };
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("gg_date")]
    public DateOnly? Date { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("gg_description")]
    public string? Description { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("gg_duration")]
    public decimal? Duration { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("gg_timesheetactivity_channel")]
    public int? ChannelCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; init; }
}
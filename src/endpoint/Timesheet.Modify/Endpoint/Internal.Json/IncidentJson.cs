using GarageGroup.Infra;
using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class IncidentJson : IProjectJson, IProjectDataverseInputBuilder
{
    private const string EntityPluralName = "incidents";

    private const string FieldProjectName = "title";

    public static DataverseEntityGetIn BuildDataverseEntityGetIn(Guid incidentId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(incidentId),
            selectFields: [FieldProjectName])
        {
            CallerObjectId = callerObjectId
        };

    [JsonPropertyName("incidentid")]
    public Guid Id { get; init; }

    [JsonPropertyName(FieldProjectName)]
    public string? ProjectName { get; init; }

    string? IProjectJson.Name
        =>
        ProjectName;

    string IProjectJson.LookupValue
        =>
        $"/{EntityPluralName}({Id:D})";

    string IProjectJson.LookupEntity { get; }
        =
        "incident";
}

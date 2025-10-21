using GarageGroup.Infra;
using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson : IProjectJson, IProjectDataverseInputBuilder
{
    private const string EntityPluralName = "gg_projects";

    private const string FieldProjectName = "gg_name";

    public static DataverseEntityGetIn BuildDataverseEntityGetIn(Guid projectId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(projectId),
            selectFields: [FieldProjectName])
        {
            CallerObjectId = callerObjectId
        };

    [JsonPropertyName("gg_projectid")]
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
        "gg_project";
}

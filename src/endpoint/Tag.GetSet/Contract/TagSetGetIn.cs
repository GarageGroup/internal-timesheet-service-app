using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static TagSetGetMetadata;

public sealed record class TagSetGetIn
{
    public TagSetGetIn(
        [ClaimIn("oid")] Guid azureUserId,
        [JsonBodyIn, SwaggerDescription(In.ProjectIdDescription), StringExample(In.ProjectIdExample)] Guid projectId)
    {
        AzureUserId = azureUserId;
        ProjectId = projectId;
    }

    public Guid AzureUserId { get; }

    public Guid ProjectId { get; }
}
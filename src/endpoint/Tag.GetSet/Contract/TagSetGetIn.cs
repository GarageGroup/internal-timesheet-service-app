using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static TagSetGetMetadata;

public sealed record class TagSetGetIn
{
    public TagSetGetIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.ProjectIdDescription), StringExample(In.ProjectIdExample)] Guid projectId)
    {
        SystemUserId = systemUserId;
        ProjectId = projectId;
    }

    public Guid SystemUserId { get; }

    public Guid ProjectId { get; }
}
using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static ProjectSetGetMetadata;

public readonly record struct ProjectSetGetIn
{
    public ProjectSetGetIn(
        [ClaimIn("oid")] Guid systemUserId,
        [QueryIn, SwaggerDescription(In.TopDescription), IntegerExample(In.TopExample)] int? top = null)
    {
        SystemUserId = systemUserId;
        Top = top;
    }

    public Guid SystemUserId { get; }

    // All items are returned if the value is absent or is not positive
    public int? Top { get; }
}
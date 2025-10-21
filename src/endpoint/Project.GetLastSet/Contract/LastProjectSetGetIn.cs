using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static LastProjectSetGetMetadata;

public readonly record struct LastProjectSetGetIn
{
    public LastProjectSetGetIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.TopDescription), IntegerExample(In.TopExample)] int? top)
    {
        SystemUserId = systemUserId;
        Top = top;
    }

    public Guid SystemUserId { get; }

    public int? Top { get; }
}
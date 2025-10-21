using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static PeriodSetGetMetadata;

public readonly record struct PeriodSetGetOut
{
    [JsonBodyOut, SwaggerDescription(Out.PeriodsDescription)]
    public required FlatArray<PeriodItem> Periods { get; init; }
}
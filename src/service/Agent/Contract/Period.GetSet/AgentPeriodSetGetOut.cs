using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentPeriodSetGetOut
{
    public required FlatArray<AgentPeriodItem> Periods { get; init; }
}

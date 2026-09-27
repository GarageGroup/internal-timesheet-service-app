using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentLastProjectSetGetOut
{
    public required FlatArray<AgentProjectItem> Projects { get; init; }
}

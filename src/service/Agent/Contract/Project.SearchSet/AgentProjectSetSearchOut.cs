using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentProjectSetSearchOut
{
    public required FlatArray<AgentProjectItem> Projects { get; init; }
}

using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentTagSetGetOut
{
    public required FlatArray<string> Tags { get; init; }
}

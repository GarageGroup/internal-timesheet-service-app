using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentTimesheetSetGetOut
{
    public required FlatArray<AgentTimesheetSetGetItem> Timesheets { get; init; }
}

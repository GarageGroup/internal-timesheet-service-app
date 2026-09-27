using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentTimesheetSetGetIn(DateOnly DateFrom, DateOnly DateTo);

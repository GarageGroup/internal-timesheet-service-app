using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentTimesheetDeletePrepareIn(Guid TimesheetId, DateOnly Date);

using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetDeleteConfirmOut(Guid ActionId, DateOnly Date);

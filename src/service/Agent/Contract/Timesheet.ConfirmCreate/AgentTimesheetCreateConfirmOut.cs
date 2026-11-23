using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreateConfirmOut(Guid ActionId, DateOnly Date);

using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetUpdatePrepareOption
{
    public required TimeSpan ApprovalTtl { get; init; }
}

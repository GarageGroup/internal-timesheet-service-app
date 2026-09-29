using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetDeletePrepareOption
{
    public required TimeSpan ApprovalTtl { get; init; }
}

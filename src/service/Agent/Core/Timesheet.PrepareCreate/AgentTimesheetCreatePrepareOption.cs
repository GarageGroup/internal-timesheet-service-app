using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreatePrepareOption
{
    public required TimeSpan ApprovalTtl { get; init; }

    public required int ProjectSearchTop { get; init; }
}

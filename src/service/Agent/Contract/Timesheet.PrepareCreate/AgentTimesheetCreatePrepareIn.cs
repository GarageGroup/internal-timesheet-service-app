using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreatePrepareIn
{
    public AgentTimesheetCreatePrepareIn(
        DateOnly date,
        Guid projectId,
        decimal duration,
        [AllowNull] string description)
    {
        Date = date;
        ProjectId = projectId;
        Duration = duration;
        Description = description.OrNullIfWhiteSpace();
    }

    public DateOnly Date { get; }

    public Guid ProjectId { get; }

    public decimal Duration { get; }

    public string? Description { get; }
}

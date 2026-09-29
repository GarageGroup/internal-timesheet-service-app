using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreatePrepareIn
{
    public AgentTimesheetCreatePrepareIn(
        DateOnly date,
        Guid projectId,
        [AllowNull] string projectName,
        ProjectType projectType,
        decimal duration,
        [AllowNull] string description)
    {
        Date = date;
        ProjectId = projectId;
        ProjectName = projectName.OrEmpty();
        ProjectType = projectType;
        Duration = duration;
        Description = description.OrNullIfWhiteSpace();
    }

    public DateOnly Date { get; }

    public Guid ProjectId { get; }

    public string ProjectName { get; }

    public ProjectType ProjectType { get; }

    public decimal Duration { get; }

    public string? Description { get; }
}

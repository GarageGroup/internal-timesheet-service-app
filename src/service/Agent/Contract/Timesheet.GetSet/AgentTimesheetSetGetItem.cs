using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetSetGetItem
{
    public AgentTimesheetSetGetItem(
        Guid id,
        Guid projectId,
        ProjectType projectType,
        [AllowNull] string projectName,
        decimal duration,
        [AllowNull] string description,
        bool isActive,
        DateOnly date)
    {
        Id = id;
        ProjectId = projectId;
        ProjectType = projectType;
        ProjectName = projectName.OrEmpty();
        Duration = duration;
        Description = description.OrEmpty();
        IsActive = isActive;
        Date = date;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public ProjectType ProjectType { get; }

    public string ProjectName { get; }

    public string? ProjectComment { get; init; }

    public decimal Duration { get; }

    public string Description { get; }

    public bool IsActive { get; }

    public DateOnly Date { get; }
}

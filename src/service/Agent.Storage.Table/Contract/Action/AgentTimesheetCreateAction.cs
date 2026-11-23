using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreateAction
{
    public AgentTimesheetCreateAction(
        Guid actionId,
        DateOnly date,
        Guid projectId,
        [AllowNull] string projectName,
        ProjectType projectType,
        decimal duration,
        [AllowNull] string description,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        AgentActionState state = AgentActionState.Pending,
        [AllowNull] string version = null)
    {
        ActionId = actionId;
        Date = date;
        ProjectId = projectId;
        ProjectName = projectName.OrEmpty();
        ProjectType = projectType;
        Duration = duration;
        Description = description.OrNullIfWhiteSpace();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        State = state;
        Version = version.OrNullIfEmpty();
    }

    public Guid ActionId { get; }

    public DateOnly Date { get; }

    public Guid ProjectId { get; }

    public string ProjectName { get; }

    public ProjectType ProjectType { get; }

    public decimal Duration { get; }

    public string? Description { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public AgentActionState State { get; init; }

    public string? Version { get; init; }
}

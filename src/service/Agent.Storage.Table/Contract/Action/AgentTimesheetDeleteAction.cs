using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetDeleteAction
{
    public AgentTimesheetDeleteAction(
        Guid actionId,
        Guid timesheetId,
        DateOnly date,
        [AllowNull] string projectName,
        decimal duration,
        [AllowNull] string description,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        AgentActionState state = AgentActionState.Pending,
        [AllowNull] string version = null)
    {
        ActionId = actionId;
        TimesheetId = timesheetId;
        Date = date;
        ProjectName = projectName.OrEmpty();
        Duration = duration;
        Description = description.OrEmpty();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        State = state;
        Version = version.OrNullIfEmpty();
    }

    public Guid ActionId { get; }

    public Guid TimesheetId { get; }

    public DateOnly Date { get; }

    public string ProjectName { get; }

    public decimal Duration { get; }

    public string Description { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public AgentActionState State { get; init; }

    public string? Version { get; init; }
}

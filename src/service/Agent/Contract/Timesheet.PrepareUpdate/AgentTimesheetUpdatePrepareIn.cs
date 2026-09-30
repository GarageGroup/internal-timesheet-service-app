using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetUpdatePrepareIn
{
    public AgentTimesheetUpdatePrepareIn(
        Guid timesheetId,
        DateOnly sourceDate,
        DateOnly? date,
        Guid? projectId,
        decimal? duration,
        [AllowNull] string description)
    {
        TimesheetId = timesheetId;
        SourceDate = sourceDate;
        Date = date;
        ProjectId = projectId;
        Duration = duration;
        Description = description.OrNullIfWhiteSpace();
    }

    public Guid TimesheetId { get; }

    public DateOnly SourceDate { get; }

    public DateOnly? Date { get; }

    public Guid? ProjectId { get; }

    public decimal? Duration { get; }

    public string? Description { get; }
}

using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetModifyMetadata;

public sealed record class TimesheetUpdateIn
{
    public TimesheetUpdateIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.TimesheetIdDescription), StringExample(In.TimesheetIdExample)] Guid timesheetId,
        [JsonBodyIn, SwaggerDescription(In.DateDescription)] DateOnly? date,
        [JsonBodyIn, SwaggerDescription(In.ProjectDescription)] TimesheetProject? project,
        [JsonBodyIn, SwaggerDescription(In.DurationDescription), IntegerExample(In.DurationExample)] decimal? duration,
        [JsonBodyIn, SwaggerDescription(In.DescriptionDescription), StringExample(In.DescriptionExample)] string? description)
    {
        SystemUserId = systemUserId;
        TimesheetId = timesheetId;
        Date = date;
        Project = project;
        Duration = duration;
        Description = description.OrNullIfWhiteSpace();
    }

    public Guid SystemUserId { get; }

    public Guid TimesheetId { get; }

    public DateOnly? Date { get; }

    public TimesheetProject? Project { get; }

    public decimal? Duration { get; }

    public string? Description { get; }
}
using GarageGroup.Infra;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetModifyMetadata;

public sealed record class TimesheetCreateIn
{
    public TimesheetCreateIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.DateDescription)] DateOnly date,
        [JsonBodyIn, SwaggerDescription(In.ProjectDescription)] TimesheetProject project,
        [JsonBodyIn, SwaggerDescription(In.DurationDescription), IntegerExample(In.DurationExample)] decimal duration,
        [JsonBodyIn, SwaggerDescription(In.DescriptionDescription), StringExample(In.DescriptionExample)] [AllowNull] string description)
    {
        SystemUserId = systemUserId;
        Date = date;
        Description = description.OrNullIfEmpty();
        Duration = duration;
        Project = project;
    }

    public Guid SystemUserId { get; }

    public DateOnly Date { get; }

    public TimesheetProject Project { get; }

    public decimal Duration { get; }

    public string? Description { get; }
}
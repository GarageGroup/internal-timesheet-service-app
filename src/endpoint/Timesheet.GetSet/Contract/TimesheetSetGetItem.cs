using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetSetGetMetadata;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
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

    [SwaggerDescription(Out.IdDescription)]
    [StringExample(Out.IdExample)]
    public Guid Id { get; }

    [SwaggerDescription(Out.ProjectIdDescription)]
    [StringExample(Out.ProjectIdExample)]
    public Guid ProjectId { get; }

    [SwaggerDescription(Out.ProjectTypeDescription)]
    [StringExample(Out.ProjectTypeExample)]
    public ProjectType ProjectType { get; }

    [SwaggerDescription(Out.ProjectNameDescription)]
    [StringExample(Out.ProjectNameExample)]
    public string ProjectName { get; }

    [SwaggerDescription(Out.ProjectCommentDescription)]
    [StringExample(Out.ProjectCommentExample)]
    public string? ProjectComment { get; init; }

    [SwaggerDescription(Out.DurationDescription)]
    [IntegerExample(Out.DurationExample)]
    public decimal Duration { get; }

    [SwaggerDescription(Out.DescriptionDescription)]
    [StringExample(Out.DescriptionExample)]
    public string Description { get; }

    [SwaggerDescription(Out.TimesheetIsActiveDescription)]
    public bool IsActive { get; }

    [SwaggerDescription(Out.DateDescription)]
    public DateOnly Date { get; }
}

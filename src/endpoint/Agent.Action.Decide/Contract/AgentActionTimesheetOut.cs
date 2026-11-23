using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentActionTimesheetOut
{
    public AgentActionTimesheetOut(
        Guid id,
        string projectName,
        string projectType,
        decimal duration,
        string description,
        bool isActive)
    {
        Id = id;
        ProjectName = projectName;
        ProjectType = projectType;
        Duration = duration;
        Description = description;
        IsActive = isActive;
    }

    [JsonBodyOut]
    public Guid Id { get; }

    [JsonBodyOut]
    public string ProjectName { get; }

    [JsonBodyOut]
    public string ProjectType { get; }

    [JsonBodyOut]
    public decimal Duration { get; }

    [JsonBodyOut]
    public string Description { get; }

    [JsonBodyOut]
    public bool IsActive { get; }
}

using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetCreatePrepareOut(
    Guid ActionId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    ProjectType ProjectType,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);

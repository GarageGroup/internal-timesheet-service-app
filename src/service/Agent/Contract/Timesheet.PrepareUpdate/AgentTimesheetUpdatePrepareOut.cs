using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetUpdatePrepareOut(
    Guid ActionId,
    Guid TimesheetId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    ProjectType ProjectType,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);

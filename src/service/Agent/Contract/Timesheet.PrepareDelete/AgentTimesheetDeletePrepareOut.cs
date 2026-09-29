using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetDeletePrepareOut(
    Guid ActionId,
    Guid TimesheetId,
    DateOnly Date,
    string ProjectName,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);

using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetDeleteMetadata;

public readonly record struct TimesheetDeleteIn
{
    public TimesheetDeleteIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.IdDescription), StringExample(In.IdExample)] Guid timesheetId)
    {
        SystemUserId = systemUserId;
        TimesheetId = timesheetId;
    }

    public Guid SystemUserId { get; }

    public Guid TimesheetId { get; }
}
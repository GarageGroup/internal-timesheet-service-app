using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetSetGetMetadata;

public sealed record class TimesheetSetGetIn
{
    public TimesheetSetGetIn(
        [ClaimIn("oid")] Guid systemUserId,
        [JsonBodyIn, SwaggerDescription(In.DateFromDescription)] DateOnly dateFrom,
        [JsonBodyIn, SwaggerDescription(In.DateToDescription)] DateOnly dateTo)
    {
        SystemUserId = systemUserId;
        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    public Guid SystemUserId { get; }

    public DateOnly DateFrom { get; }

    public DateOnly DateTo { get; }
}
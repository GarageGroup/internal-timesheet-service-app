using System;

namespace GarageGroup.Internal.Timesheet;

// The production calendar says which days are working ones. Without the address the calendar is not requested:
// Monday - Friday are working days then
public sealed record class WorkCalendarOption
{
    public Uri? BaseAddress { get; init; }

    public string? SubscriptionKey { get; init; }

    public string Country { get; init; } = "RU";
}

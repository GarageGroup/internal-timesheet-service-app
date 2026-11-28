using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITodayProvider
{
    // The earliest date that is "today" somewhere in the world
    DateOnly EarliestToday { get; }
}

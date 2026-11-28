using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITodayProvider
{
    // The latest date that is "today" somewhere in the world
    DateOnly Today { get; }
}

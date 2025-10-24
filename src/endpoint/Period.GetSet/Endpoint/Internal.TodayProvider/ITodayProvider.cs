using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITodayProvider
{
    DateTime Today { get; }
}
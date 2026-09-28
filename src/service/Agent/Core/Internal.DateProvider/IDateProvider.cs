using System;

namespace GarageGroup.Internal.Timesheet;

internal interface IDateProvider
{
    DateOnly Today { get; }
}

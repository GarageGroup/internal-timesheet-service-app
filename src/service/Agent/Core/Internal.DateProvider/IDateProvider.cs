using System;

namespace GarageGroup.Internal.Timesheet;

internal interface IDateProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

// The temporary rule, used until periods are closed in Dataverse: only the current period (the calendar month of today)
// is open, and the previous one stays open for the first working days after its end. Every earlier period is closed.
// A period that has not started yet is not closed
public interface IOpenPeriodApi
{
    // Returns the period of the date if time entries of the period can no longer be created, changed or deleted
    ValueTask<ClosedPeriod?> FindClosedPeriodAsync(DateOnly date, CancellationToken cancellationToken);
}

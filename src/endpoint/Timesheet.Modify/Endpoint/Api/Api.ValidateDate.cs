using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetModifyApi
{
    private const string DateFormat = "yyyy-MM-dd";

    // newDate is the date the entry gets, existingDate is the date the entry has now.
    // Time cannot be logged for a future date, and an entry cannot be put into a closed period or changed in it
    private async ValueTask<Result<T, Failure<DateFailureCode>>> ValidateDateAsync<T>(
        T value, DateOnly? newDate, DateOnly? existingDate, CancellationToken cancellationToken)
    {
        if (newDate > todayProvider.Today)
        {
            return Failure.Create(
                DateFailureCode.FutureDate, $"Time cannot be logged for a future date: {newDate.Value.ToString(DateFormat)}.");
        }

        var dates = new[] { existingDate, newDate }.Where(static date => date is not null).Select(static date => date!.Value).Distinct().ToArray();
        if (dates.Length is 0)
        {
            return value;
        }

        var periods = await dataverseApi.GetEntitySetAsync<PeriodJson>(
            PeriodJson.BuildClosedPeriodSetGetInput(dates), cancellationToken).ConfigureAwait(false);

        if (periods.IsFailure)
        {
            return periods.FailureOrThrow().WithFailureCode(DateFailureCode.Unknown);
        }

        if (periods.SuccessOrThrow().Value.IsEmpty)
        {
            return value;
        }

        var period = periods.SuccessOrThrow().Value[0];
        var date = dates.FirstOrDefault(date => date >= period.From && date <= period.To, dates[0]);

        return Failure.Create(
            DateFailureCode.PeriodClosed,
            $"The period {period.Name} ({period.From.ToString(DateFormat)} - {period.To.ToString(DateFormat)}) is closed: " +
            $"time entries of {date.ToString(DateFormat)} cannot be created or changed.");
    }

    private enum DateFailureCode
    {
        Unknown,

        FutureDate,

        PeriodClosed
    }
}

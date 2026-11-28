using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFunc
{
    private const string DateFormat = "yyyy-MM-dd";

    public ValueTask<Result<Unit, Failure<TimesheetDeleteFailureCode>>> InvokeAsync(
        TimesheetDeleteIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeValue(
            ValidatePeriodAsync)
        .MapSuccess(
            static @in => TimesheetJson.BuildDataverseDeleteInput(
                timesheetId: @in.TimesheetId,
                callerObjectId: @in.SystemUserId))
        .ForwardValue(
            DeleteAsync);

    // The entry is read first: an entry of a closed period cannot be deleted
    private async ValueTask<Result<TimesheetDeleteIn, Failure<TimesheetDeleteFailureCode>>> ValidatePeriodAsync(
        TimesheetDeleteIn input, CancellationToken cancellationToken)
    {
        var timesheet = await dataverseApi.GetEntityAsync<TimesheetJson>(
            TimesheetJson.BuildDataverseGetInput(input.TimesheetId, input.SystemUserId), cancellationToken).ConfigureAwait(false);

        if (timesheet.IsFailure)
        {
            // An absent entry goes on to the deletion, which treats it as already deleted
            var failure = timesheet.FailureOrThrow();
            return failure.FailureCode is DataverseFailureCode.RecordNotFound ? input : failure.MapFailureCode(MapFailureCode);
        }

        var date = timesheet.SuccessOrThrow().Value?.Date;
        if (date is null)
        {
            return input;
        }

        // The temporary rule: only the current period is open, and the previous one for some working days after its end
        var closedPeriod = await openPeriodApi.FindClosedPeriodAsync(date.Value, cancellationToken).ConfigureAwait(false);
        if (closedPeriod is not null)
        {
            return CreatePeriodClosedFailure(closedPeriod.Name, closedPeriod.From, closedPeriod.To, date.Value);
        }

        var periods = await dataverseApi.GetEntitySetAsync<PeriodJson>(
            PeriodJson.BuildClosedPeriodSetGetInput(date.Value), cancellationToken).ConfigureAwait(false);

        if (periods.IsFailure)
        {
            return periods.FailureOrThrow().WithFailureCode(TimesheetDeleteFailureCode.Unknown);
        }

        if (periods.SuccessOrThrow().Value.IsEmpty)
        {
            return input;
        }

        var period = periods.SuccessOrThrow().Value[0];
        return CreatePeriodClosedFailure(period.Name, period.From, period.To, date.Value);
    }

    private static Failure<TimesheetDeleteFailureCode> CreatePeriodClosedFailure(string? name, DateOnly from, DateOnly to, DateOnly date)
        =>
        Failure.Create(
            TimesheetDeleteFailureCode.PeriodClosed,
            $"The period {name} ({from.ToString(DateFormat)} - {to.ToString(DateFormat)}) is closed: " +
            $"time entries of {date.ToString(DateFormat)} cannot be deleted.");

    private ValueTask<Result<Unit, Failure<TimesheetDeleteFailureCode>>> DeleteAsync(
        DataverseEntityDeleteIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeValue(
            dataverseApi.DeleteEntityAsync)
        .Recover(
            MapFailure);

    private static Result<Unit, Failure<TimesheetDeleteFailureCode>> MapFailure(Failure<DataverseFailureCode> failure)
        =>
        failure.FailureCode switch
        {
            DataverseFailureCode.RecordNotFound => default(Unit),
            _ => failure.MapFailureCode(MapFailureCode)
        };

    private static TimesheetDeleteFailureCode MapFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.CannotUpdateBecauseItIsReadOnly => TimesheetDeleteFailureCode.BadRequest,
            DataverseFailureCode.IsvAborted => TimesheetDeleteFailureCode.BadRequest,
            _ => TimesheetDeleteFailureCode.Unknown
        };
}
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

        return Failure.Create(
            TimesheetDeleteFailureCode.PeriodClosed,
            $"The period {period.Name} ({period.From.ToString(DateFormat)} - {period.To.ToString(DateFormat)}) is closed: " +
            $"time entries of {date.Value.ToString(DateFormat)} cannot be deleted.");
    }

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
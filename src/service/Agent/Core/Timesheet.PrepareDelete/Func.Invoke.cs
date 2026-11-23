using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetDeletePrepareFunc
{
    public ValueTask<Result<AgentTimesheetDeletePrepareOut, Failure<AgentTimesheetDeletePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetDeletePrepareIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .ForwardValue(
            (@in, token) => PrepareAsync(context, @in, token));

    private static Result<AgentTimesheetDeletePrepareIn, Failure<AgentTimesheetDeletePrepareFailureCode>> ValidateInput(
        AgentTimesheetDeletePrepareIn input)
        =>
        input.TimesheetId == Guid.Empty
        ? Failure.Create(AgentTimesheetDeletePrepareFailureCode.InvalidTimesheet, "Timesheet ID is empty")
        : input;

    private async ValueTask<Result<AgentTimesheetDeletePrepareOut, Failure<AgentTimesheetDeletePrepareFailureCode>>> PrepareAsync(
        AgentUserContext context,
        AgentTimesheetDeletePrepareIn input,
        CancellationToken cancellationToken)
    {
        var timesheetSetResult = await timesheetSetGetFunc.InvokeAsync(
            context,
            new(input.Date, input.Date),
            cancellationToken).ConfigureAwait(false);

        if (timesheetSetResult.IsFailure)
        {
            return timesheetSetResult.FailureOrThrow().WithFailureCode(AgentTimesheetDeletePrepareFailureCode.Unknown);
        }

        var timesheet = timesheetSetResult.SuccessOrThrow().Timesheets.AsEnumerable().FirstOrDefault(
            item => item.Id == input.TimesheetId && item.Date == input.Date);

        if (timesheet is null)
        {
            return Failure.Create(AgentTimesheetDeletePrepareFailureCode.NotFound, "Timesheet is not available");
        }

        if (timesheet.IsActive is false)
        {
            return Failure.Create(AgentTimesheetDeletePrepareFailureCode.ReadOnly, "Timesheet is read-only");
        }

        var createdAt = dateProvider.UtcNow;
        var action = new AgentTimesheetDeleteAction(
            Guid.NewGuid(),
            timesheet.Id,
            timesheet.Date,
            timesheet.ProjectName,
            timesheet.Duration,
            timesheet.Description,
            createdAt,
            createdAt.Add(option.ApprovalTtl));

        var storeResult = await actionStore.CreateAsync(context, action, cancellationToken).ConfigureAwait(false);
        if (storeResult.IsFailure)
        {
            return storeResult.FailureOrThrow().FailureCode is AgentActionStoreFailureCode.Conflict
                ? Failure.Create(AgentTimesheetDeletePrepareFailureCode.Conflict, "Agent action already exists")
                : storeResult.FailureOrThrow().WithFailureCode(AgentTimesheetDeletePrepareFailureCode.Unknown);
        }

        return new AgentTimesheetDeletePrepareOut(
            action.ActionId,
            action.TimesheetId,
            action.Date,
            action.ProjectName,
            action.Duration,
            action.Description,
            action.ExpiresAt);
    }
}

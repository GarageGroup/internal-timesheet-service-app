using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetDeleteConfirmFunc
{
    public ValueTask<Result<AgentTimesheetDeleteConfirmOut, Failure<AgentTimesheetDeleteConfirmFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            actionId, cancellationToken)
        .Pipe(
            ValidateActionId)
        .ForwardValue(
            (id, token) => ConfirmAsync(context, id, token));

    private static Result<Guid, Failure<AgentTimesheetDeleteConfirmFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetDeleteConfirmFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetDeleteConfirmOut, Failure<AgentTimesheetDeleteConfirmFailureCode>>> ConfirmAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
    {
        var actionResult = await actionStore.GetDeleteAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (actionResult.IsFailure)
        {
            return actionResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        var action = actionResult.SuccessOrThrow();
        if (action is null)
        {
            return Failure.Create(AgentTimesheetDeleteConfirmFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetDeleteConfirmFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetDeleteConfirmFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetDeleteConfirmFailureCode.Conflict, "Agent action version is missing");
        }

        if (dateProvider.UtcNow >= action.ExpiresAt)
        {
            var expireResult = await actionStore.UpdateStateAsync(
                context,
                action.ActionId,
                action.Version,
                AgentActionState.Pending,
                AgentActionState.Expired,
                cancellationToken).ConfigureAwait(false);

            if (expireResult.IsFailure)
            {
                return expireResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
            }

            return Failure.Create(AgentTimesheetDeleteConfirmFailureCode.Expired, "Agent action has expired");
        }

        var updateResult = await actionStore.UpdateStateAsync(
            context,
            action.ActionId,
            action.Version,
            AgentActionState.Pending,
            AgentActionState.Executing,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            return updateResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        return await ExecuteAsync(context, action, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Result<AgentTimesheetDeleteConfirmOut, Failure<AgentTimesheetDeleteConfirmFailureCode>>> ExecuteAsync(
        AgentUserContext context,
        AgentTimesheetDeleteAction action,
        CancellationToken cancellationToken)
    {
        Result<Unit, Failure<TimesheetDeleteFailureCode>> deleteResult;
        try
        {
            deleteResult = await timesheetDeleteFunc.InvokeAsync(
                new(context.EntraObjectId, action.TimesheetId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _ = await TryUpdateFinalStateAsync(
                context,
                action.ActionId,
                AgentActionState.Indeterminate,
                CancellationToken.None).ConfigureAwait(false);

            return Failure.Create(
                AgentTimesheetDeleteConfirmFailureCode.Indeterminate,
                "Timesheet deletion result is indeterminate",
                exception);
        }

        if (deleteResult.IsSuccess)
        {
            var successUpdateResult = await TryUpdateFinalStateAsync(
                context,
                action.ActionId,
                AgentActionState.Succeeded,
                CancellationToken.None).ConfigureAwait(false);

            if (successUpdateResult.IsFailure)
            {
                return successUpdateResult.FailureOrThrow();
            }

            return new AgentTimesheetDeleteConfirmOut(action.ActionId);
        }

        var deleteFailure = deleteResult.FailureOrThrow();
        var failureCode = MapDeleteFailureCode(deleteFailure.FailureCode);
        var finalState = failureCode is AgentTimesheetDeleteConfirmFailureCode.Indeterminate
            ? AgentActionState.Indeterminate
            : AgentActionState.Failed;
        var failureUpdateResult = await TryUpdateFinalStateAsync(
            context,
            action.ActionId,
            finalState,
            CancellationToken.None).ConfigureAwait(false);

        if (failureUpdateResult.IsFailure)
        {
            return failureUpdateResult.FailureOrThrow();
        }

        return deleteFailure.MapFailureCode(MapDeleteFailureCode);
    }

    private async ValueTask<Result<Unit, Failure<AgentTimesheetDeleteConfirmFailureCode>>> TryUpdateFinalStateAsync(
        AgentUserContext context,
        Guid actionId,
        AgentActionState finalState,
        CancellationToken cancellationToken)
    {
        var executingResult = await actionStore.GetDeleteAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (executingResult.IsFailure)
        {
            return Failure.Create(
                AgentTimesheetDeleteConfirmFailureCode.Indeterminate,
                "Failed to load executing agent action",
                executingResult.FailureOrThrow().SourceException);
        }

        var executingAction = executingResult.SuccessOrThrow();
        if (executingAction is null ||
            executingAction.State is not AgentActionState.Executing ||
            string.IsNullOrEmpty(executingAction.Version))
        {
            return Failure.Create(
                AgentTimesheetDeleteConfirmFailureCode.Indeterminate,
                "Executing agent action state is unavailable");
        }

        var updateResult = await actionStore.UpdateStateAsync(
            context,
            actionId,
            executingAction.Version,
            AgentActionState.Executing,
            finalState,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            return Failure.Create(
                AgentTimesheetDeleteConfirmFailureCode.Indeterminate,
                "Failed to persist agent action execution result",
                updateResult.FailureOrThrow().SourceException);
        }

        return Unit.Value;
    }

    private static AgentTimesheetDeleteConfirmFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetDeleteConfirmFailureCode.Conflict,
            _ => AgentTimesheetDeleteConfirmFailureCode.Unknown
        };

    private static AgentTimesheetDeleteConfirmFailureCode MapDeleteFailureCode(TimesheetDeleteFailureCode failureCode)
        =>
        failureCode switch
        {
            TimesheetDeleteFailureCode.BadRequest => AgentTimesheetDeleteConfirmFailureCode.BadRequest,
            _ => AgentTimesheetDeleteConfirmFailureCode.Indeterminate
        };
}

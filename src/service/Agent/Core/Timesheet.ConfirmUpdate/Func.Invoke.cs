using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetUpdateConfirmFunc
{
    public ValueTask<Result<AgentTimesheetUpdateConfirmOut, Failure<AgentTimesheetUpdateConfirmFailureCode>>> InvokeAsync(
        AgentUserContext context, Guid actionId, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(actionId, cancellationToken)
        .Pipe(ValidateActionId)
        .ForwardValue((id, token) => ConfirmAsync(context, id, token));

    private static Result<Guid, Failure<AgentTimesheetUpdateConfirmFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetUpdateConfirmFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetUpdateConfirmOut, Failure<AgentTimesheetUpdateConfirmFailureCode>>> ConfirmAsync(
        AgentUserContext context, Guid actionId, CancellationToken cancellationToken)
    {
        var actionResult = await actionStore.GetUpdateAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (actionResult.IsFailure)
        {
            return actionResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        var action = actionResult.SuccessOrThrow();
        if (action is null)
        {
            return Failure.Create(AgentTimesheetUpdateConfirmFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetUpdateConfirmFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetUpdateConfirmFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetUpdateConfirmFailureCode.Conflict, "Agent action version is missing");
        }

        if (dateProvider.UtcNow >= action.ExpiresAt)
        {
            var expireResult = await actionStore.UpdateStateAsync(
                context, action.ActionId, action.Version, AgentActionState.Pending, AgentActionState.Expired, cancellationToken)
                .ConfigureAwait(false);

            return expireResult.IsFailure
                ? expireResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode)
                : Failure.Create(AgentTimesheetUpdateConfirmFailureCode.Expired, "Agent action has expired");
        }

        var executingResult = await actionStore.UpdateStateAsync(
            context, action.ActionId, action.Version, AgentActionState.Pending, AgentActionState.Executing, cancellationToken)
            .ConfigureAwait(false);
        if (executingResult.IsFailure)
        {
            return executingResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        return await ExecuteAsync(context, action, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Result<AgentTimesheetUpdateConfirmOut, Failure<AgentTimesheetUpdateConfirmFailureCode>>> ExecuteAsync(
        AgentUserContext context, AgentTimesheetUpdateAction action, CancellationToken cancellationToken)
    {
        Result<Unit, Failure<TimesheetUpdateFailureCode>> updateResult;
        try
        {
            updateResult = await timesheetUpdateFunc.UpdateAsync(
                new(
                    context.EntraObjectId,
                    action.TimesheetId,
                    action.Date,
                    new TimesheetProject(action.ProjectId, action.ProjectType),
                    action.Duration,
                    action.Description),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _ = await TryUpdateFinalStateAsync(context, action.ActionId, AgentActionState.Indeterminate).ConfigureAwait(false);

            return Failure.Create(
                AgentTimesheetUpdateConfirmFailureCode.Indeterminate, "Timesheet update result is indeterminate", exception);
        }

        var finalState = updateResult.IsSuccess
            ? AgentActionState.Succeeded
            : MapUpdateFailureCode(updateResult.FailureOrThrow().FailureCode) is AgentTimesheetUpdateConfirmFailureCode.Indeterminate
                ? AgentActionState.Indeterminate
                : AgentActionState.Failed;
        var finalResult = await TryUpdateFinalStateAsync(context, action.ActionId, finalState).ConfigureAwait(false);
        if (finalResult.IsFailure)
        {
            return finalResult.FailureOrThrow();
        }

        return updateResult.IsSuccess
            ? new AgentTimesheetUpdateConfirmOut(action.ActionId)
            : updateResult.FailureOrThrow().MapFailureCode(MapUpdateFailureCode);
    }

    private async ValueTask<Result<Unit, Failure<AgentTimesheetUpdateConfirmFailureCode>>> TryUpdateFinalStateAsync(
        AgentUserContext context, Guid actionId, AgentActionState finalState)
    {
        var actionResult = await actionStore.GetUpdateAsync(context, actionId, CancellationToken.None).ConfigureAwait(false);
        if (actionResult.IsFailure)
        {
            return Failure.Create(
                AgentTimesheetUpdateConfirmFailureCode.Indeterminate,
                "Failed to load executing agent action",
                actionResult.FailureOrThrow().SourceException);
        }

        var action = actionResult.SuccessOrThrow();
        if (action is null || action.State is not AgentActionState.Executing || string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(
                AgentTimesheetUpdateConfirmFailureCode.Indeterminate, "Executing agent action state is unavailable");
        }

        var updateResult = await actionStore.UpdateStateAsync(
            context, actionId, action.Version, AgentActionState.Executing, finalState, CancellationToken.None).ConfigureAwait(false);

        return updateResult.IsFailure
            ? Failure.Create(
                AgentTimesheetUpdateConfirmFailureCode.Indeterminate,
                "Failed to persist agent action execution result",
                updateResult.FailureOrThrow().SourceException)
            : Unit.Value;
    }

    private static AgentTimesheetUpdateConfirmFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode is AgentActionStoreFailureCode.Conflict
        ? AgentTimesheetUpdateConfirmFailureCode.Conflict
        : AgentTimesheetUpdateConfirmFailureCode.Unknown;

    private static AgentTimesheetUpdateConfirmFailureCode MapUpdateFailureCode(TimesheetUpdateFailureCode failureCode)
        =>
        failureCode switch
        {
            TimesheetUpdateFailureCode.BadRequest => AgentTimesheetUpdateConfirmFailureCode.BadRequest,
            TimesheetUpdateFailureCode.UnexpectedProjectType => AgentTimesheetUpdateConfirmFailureCode.BadRequest,
            TimesheetUpdateFailureCode.TimesheetNotFound => AgentTimesheetUpdateConfirmFailureCode.BadRequest,
            TimesheetUpdateFailureCode.ProjectNotFound => AgentTimesheetUpdateConfirmFailureCode.ProjectNotFound,
            _ => AgentTimesheetUpdateConfirmFailureCode.Indeterminate
        };
}

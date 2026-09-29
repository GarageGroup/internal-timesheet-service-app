using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetCreateConfirmFunc
{
    public ValueTask<Result<AgentTimesheetCreateConfirmOut, Failure<AgentTimesheetCreateConfirmFailureCode>>> InvokeAsync(
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

    private static Result<Guid, Failure<AgentTimesheetCreateConfirmFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetCreateConfirmFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetCreateConfirmOut, Failure<AgentTimesheetCreateConfirmFailureCode>>> ConfirmAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
    {
        var actionResult = await actionStore.GetAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (actionResult.IsFailure)
        {
            return actionResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        var action = actionResult.SuccessOrThrow();
        if (action is null)
        {
            return Failure.Create(AgentTimesheetCreateConfirmFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetCreateConfirmFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetCreateConfirmFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetCreateConfirmFailureCode.Conflict, "Agent action version is missing");
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

            return Failure.Create(AgentTimesheetCreateConfirmFailureCode.Expired, "Agent action has expired");
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

    private async ValueTask<Result<AgentTimesheetCreateConfirmOut, Failure<AgentTimesheetCreateConfirmFailureCode>>> ExecuteAsync(
        AgentUserContext context,
        AgentTimesheetCreateAction action,
        CancellationToken cancellationToken)
    {
        Result<Unit, Failure<TimesheetCreateFailureCode>> createResult;
        try
        {
            createResult = await timesheetCreateFunc.CreateAsync(
                new(
                    context.EntraObjectId,
                    action.Date,
                    new(action.ProjectId, action.ProjectType),
                    action.Duration,
                    action.Description),
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
                AgentTimesheetCreateConfirmFailureCode.Indeterminate,
                "Timesheet execution result is indeterminate",
                exception);
        }

        if (createResult.IsSuccess)
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

            return new AgentTimesheetCreateConfirmOut(action.ActionId);
        }

        var createFailure = createResult.FailureOrThrow();
        var failureCode = MapCreateFailureCode(createFailure.FailureCode);
        var finalState = failureCode is AgentTimesheetCreateConfirmFailureCode.Indeterminate
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

        return createFailure.MapFailureCode(MapCreateFailureCode);
    }

    private async ValueTask<Result<Unit, Failure<AgentTimesheetCreateConfirmFailureCode>>> TryUpdateFinalStateAsync(
        AgentUserContext context,
        Guid actionId,
        AgentActionState finalState,
        CancellationToken cancellationToken)
    {
        var executingResult = await actionStore.GetAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (executingResult.IsFailure)
        {
            return Failure.Create(
                AgentTimesheetCreateConfirmFailureCode.Indeterminate,
                "Failed to load executing agent action",
                executingResult.FailureOrThrow().SourceException);
        }

        var executingAction = executingResult.SuccessOrThrow();
        if (executingAction is null ||
            executingAction.State is not AgentActionState.Executing ||
            string.IsNullOrEmpty(executingAction.Version))
        {
            return Failure.Create(
                AgentTimesheetCreateConfirmFailureCode.Indeterminate,
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
                AgentTimesheetCreateConfirmFailureCode.Indeterminate,
                "Failed to persist agent action execution result",
                updateResult.FailureOrThrow().SourceException);
        }

        return Unit.Value;
    }

    private static AgentTimesheetCreateConfirmFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetCreateConfirmFailureCode.Conflict,
            _ => AgentTimesheetCreateConfirmFailureCode.Unknown
        };

    private static AgentTimesheetCreateConfirmFailureCode MapCreateFailureCode(TimesheetCreateFailureCode failureCode)
        =>
        failureCode switch
        {
            TimesheetCreateFailureCode.BadRequest => AgentTimesheetCreateConfirmFailureCode.BadRequest,
            TimesheetCreateFailureCode.UnexpectedProjectType => AgentTimesheetCreateConfirmFailureCode.BadRequest,
            TimesheetCreateFailureCode.EmptyDescription => AgentTimesheetCreateConfirmFailureCode.BadRequest,
            TimesheetCreateFailureCode.Forbidden => AgentTimesheetCreateConfirmFailureCode.Forbidden,
            TimesheetCreateFailureCode.ProjectNotFound => AgentTimesheetCreateConfirmFailureCode.ProjectNotFound,
            _ => AgentTimesheetCreateConfirmFailureCode.Indeterminate
        };
}

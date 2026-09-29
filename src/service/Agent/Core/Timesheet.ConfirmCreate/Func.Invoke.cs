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

        return new AgentTimesheetCreateConfirmOut(action.ActionId);
    }

    private static AgentTimesheetCreateConfirmFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetCreateConfirmFailureCode.Conflict,
            _ => AgentTimesheetCreateConfirmFailureCode.Unknown
        };
}

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetDeleteCancelFunc
{
    public ValueTask<Result<AgentTimesheetDeleteCancelOut, Failure<AgentTimesheetDeleteCancelFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            actionId, cancellationToken)
        .Pipe(
            ValidateActionId)
        .ForwardValue(
            (id, token) => CancelAsync(context, id, token));

    private static Result<Guid, Failure<AgentTimesheetDeleteCancelFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetDeleteCancelFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetDeleteCancelOut, Failure<AgentTimesheetDeleteCancelFailureCode>>> CancelAsync(
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
            return Failure.Create(AgentTimesheetDeleteCancelFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetDeleteCancelFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetDeleteCancelFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetDeleteCancelFailureCode.Conflict, "Agent action version is missing");
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

            return Failure.Create(AgentTimesheetDeleteCancelFailureCode.Expired, "Agent action has expired");
        }

        var updateResult = await actionStore.UpdateStateAsync(
            context,
            action.ActionId,
            action.Version,
            AgentActionState.Pending,
            AgentActionState.Cancelled,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            return updateResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        return new AgentTimesheetDeleteCancelOut(action.ActionId);
    }

    private static AgentTimesheetDeleteCancelFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetDeleteCancelFailureCode.Conflict,
            _ => AgentTimesheetDeleteCancelFailureCode.Unknown
        };
}

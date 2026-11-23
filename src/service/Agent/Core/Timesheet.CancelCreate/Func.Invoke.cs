using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetCreateCancelFunc
{
    public ValueTask<Result<AgentTimesheetCreateCancelOut, Failure<AgentTimesheetCreateCancelFailureCode>>> InvokeAsync(
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

    private static Result<Guid, Failure<AgentTimesheetCreateCancelFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetCreateCancelFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetCreateCancelOut, Failure<AgentTimesheetCreateCancelFailureCode>>> CancelAsync(
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
            return Failure.Create(AgentTimesheetCreateCancelFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetCreateCancelFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetCreateCancelFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetCreateCancelFailureCode.Conflict, "Agent action version is missing");
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

            return Failure.Create(AgentTimesheetCreateCancelFailureCode.Expired, "Agent action has expired");
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

        return new AgentTimesheetCreateCancelOut(action.ActionId);
    }

    private static AgentTimesheetCreateCancelFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetCreateCancelFailureCode.Conflict,
            _ => AgentTimesheetCreateCancelFailureCode.Unknown
        };
}

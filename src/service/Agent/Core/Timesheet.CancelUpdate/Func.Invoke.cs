using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetUpdateCancelFunc
{
    public ValueTask<Result<AgentTimesheetUpdateCancelOut, Failure<AgentTimesheetUpdateCancelFailureCode>>> InvokeAsync(
        AgentUserContext context, Guid actionId, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(actionId, cancellationToken)
        .Pipe(ValidateActionId)
        .ForwardValue((id, token) => CancelAsync(context, id, token));

    private static Result<Guid, Failure<AgentTimesheetUpdateCancelFailureCode>> ValidateActionId(Guid actionId)
        =>
        actionId == Guid.Empty
        ? Failure.Create(AgentTimesheetUpdateCancelFailureCode.InvalidActionId, "Action ID is empty")
        : actionId;

    private async ValueTask<Result<AgentTimesheetUpdateCancelOut, Failure<AgentTimesheetUpdateCancelFailureCode>>> CancelAsync(
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
            return Failure.Create(AgentTimesheetUpdateCancelFailureCode.NotFound, "Agent action was not found");
        }

        if (action.State is AgentActionState.Expired)
        {
            return Failure.Create(AgentTimesheetUpdateCancelFailureCode.Expired, "Agent action has expired");
        }

        if (action.State is not AgentActionState.Pending)
        {
            return Failure.Create(AgentTimesheetUpdateCancelFailureCode.InvalidState, "Agent action is not pending");
        }

        if (string.IsNullOrEmpty(action.Version))
        {
            return Failure.Create(AgentTimesheetUpdateCancelFailureCode.Conflict, "Agent action version is missing");
        }

        var finalState = dateProvider.UtcNow >= action.ExpiresAt ? AgentActionState.Expired : AgentActionState.Cancelled;
        var updateResult = await actionStore.UpdateStateAsync(
            context, action.ActionId, action.Version, AgentActionState.Pending, finalState, cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            return updateResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        return finalState is AgentActionState.Expired
            ? Failure.Create(AgentTimesheetUpdateCancelFailureCode.Expired, "Agent action has expired")
            : new AgentTimesheetUpdateCancelOut(action.ActionId);
    }

    private static AgentTimesheetUpdateCancelFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode is AgentActionStoreFailureCode.Conflict
        ? AgentTimesheetUpdateCancelFailureCode.Conflict
        : AgentTimesheetUpdateCancelFailureCode.Unknown;
}

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionDecideFunc
{
    public ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> InvokeAsync(
        AgentActionDecideIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .ForwardValue(
            (@in, token) => userContextResolver.ResolveAsync(
                new(@in.BotId, @in.TelegramUserId, @in.TelegramChatId),
                token),
            static failure => failure.MapFailureCode(MapUserFailureCode))
        .ForwardValue(
            (context, token) => DecideAsync(context, input, token));

    private Result<AgentActionDecideIn, Failure<AgentActionDecideFailureCode>> ValidateInput(
        AgentActionDecideIn input)
    {
        if (option.Enabled is false)
        {
            return Failure.Create(AgentActionDecideFailureCode.WriteDisabled, "Agent write operations are disabled");
        }

        if (input.ActionId == Guid.Empty ||
            input.TelegramUpdateId <= 0 ||
            Enum.IsDefined(input.Decision) is false)
        {
            return Failure.Create(AgentActionDecideFailureCode.InvalidDecision, "Agent action decision is invalid");
        }

        return input;
    }

    private async ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> DecideAsync(
        AgentUserContext context,
        AgentActionDecideIn input,
        CancellationToken cancellationToken)
    {
        if (input.Decision is AgentActionDecision.Confirm)
        {
            var confirmResult = await confirmFunc.InvokeAsync(
                context,
                input.ActionId,
                cancellationToken).ConfigureAwait(false);

            return confirmResult.Map(
                _ => new AgentActionDecideOut(input.ActionId, input.Decision),
                static failure => failure.MapFailureCode(MapConfirmFailureCode));
        }

        var cancelResult = await cancelFunc.InvokeAsync(
            context,
            input.ActionId,
            cancellationToken).ConfigureAwait(false);

        return cancelResult.Map(
            _ => new AgentActionDecideOut(input.ActionId, input.Decision),
            static failure => failure.MapFailureCode(MapCancelFailureCode));
    }

    private static AgentActionDecideFailureCode MapUserFailureCode(AgentUserContextResolveFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentUserContextResolveFailureCode.InvalidIdentity => AgentActionDecideFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UnsupportedChat => AgentActionDecideFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UserNotLinked => AgentActionDecideFailureCode.UserNotLinked,
            AgentUserContextResolveFailureCode.AmbiguousBinding => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.BindingSignedOut => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.UserDisabled => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.MissingEntraObjectId => AgentActionDecideFailureCode.UserUnavailable,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapConfirmFailureCode(AgentTimesheetCreateConfirmFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetCreateConfirmFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetCreateConfirmFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetCreateConfirmFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetCreateConfirmFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetCreateConfirmFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            AgentTimesheetCreateConfirmFailureCode.BadRequest => AgentActionDecideFailureCode.InvalidTimesheet,
            AgentTimesheetCreateConfirmFailureCode.Forbidden => AgentActionDecideFailureCode.TimesheetForbidden,
            AgentTimesheetCreateConfirmFailureCode.ProjectNotFound => AgentActionDecideFailureCode.ProjectNotFound,
            AgentTimesheetCreateConfirmFailureCode.Indeterminate => AgentActionDecideFailureCode.Indeterminate,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapCancelFailureCode(AgentTimesheetCreateCancelFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetCreateCancelFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetCreateCancelFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetCreateCancelFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetCreateCancelFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetCreateCancelFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            _ => AgentActionDecideFailureCode.Unknown
        };
}

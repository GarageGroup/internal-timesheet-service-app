using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentMessageSendFunc
{
    public ValueTask<Result<AgentMessageSendOut, Failure<AgentMessageSendFailureCode>>> InvokeAsync(
        AgentMessageSendIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => new AgentUserIdentity(@in.BotId, @in.TelegramUserId, @in.TelegramChatId))
        .PipeValue(
            userContextResolver.ResolveAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(MapUserFailureCode))
        .ForwardValue(
            (context, token) => SendMessageAsync(context, input, token))
        .MapSuccess(
            static result => new AgentMessageSendOut(
                result.Text,
                MapPreparedCreateAction(result.PreparedCreateAction),
                MapPreparedDeleteAction(result.PreparedDeleteAction)));

    private ValueTask<Result<AgentMessageOut, Failure<AgentMessageSendFailureCode>>> SendMessageAsync(
        AgentUserContext context,
        AgentMessageSendIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            new AgentConversationMessageIn(input.Text, input.Locale), cancellationToken)
        .PipeValue(
            (message, token) => messageFunc.InvokeAsync(context, message, token))
        .MapFailure(
            static failure => failure.MapFailureCode(MapMessageFailureCode));

    private static AgentMessageSendFailureCode MapUserFailureCode(AgentUserContextResolveFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentUserContextResolveFailureCode.InvalidIdentity => AgentMessageSendFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UnsupportedChat => AgentMessageSendFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UserNotLinked => AgentMessageSendFailureCode.UserNotLinked,
            AgentUserContextResolveFailureCode.AmbiguousBinding => AgentMessageSendFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.BindingSignedOut => AgentMessageSendFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.UserDisabled => AgentMessageSendFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.MissingEntraObjectId => AgentMessageSendFailureCode.UserUnavailable,
            _ => AgentMessageSendFailureCode.Unknown
        };

    private static AgentMessageSendCreateActionOut? MapPreparedCreateAction(AgentTimesheetCreatePrepareOut? action)
        =>
        action is null
            ? null
            : new()
            {
                ActionId = action.ActionId,
                Date = action.Date,
                ProjectId = action.ProjectId,
                ProjectName = action.ProjectName,
                ProjectType = (int)action.ProjectType,
                Duration = action.Duration,
                Description = action.Description,
                ExpiresAt = action.ExpiresAt
            };

    private static AgentMessageSendDeleteActionOut? MapPreparedDeleteAction(AgentTimesheetDeletePrepareOut? action)
        =>
        action is null
            ? null
            : new()
            {
                ActionId = action.ActionId,
                TimesheetId = action.TimesheetId,
                Date = action.Date,
                ProjectName = action.ProjectName,
                Duration = action.Duration,
                Description = action.Description,
                ExpiresAt = action.ExpiresAt
            };

    private static AgentMessageSendFailureCode MapMessageFailureCode(AgentConversationMessageFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentConversationMessageFailureCode.InvalidMessage => AgentMessageSendFailureCode.InvalidMessage,
            AgentConversationMessageFailureCode.EmptyResponse => AgentMessageSendFailureCode.EmptyResponse,
            AgentConversationMessageFailureCode.ConversationConflict => AgentMessageSendFailureCode.ConversationConflict,
            _ => AgentMessageSendFailureCode.Unknown
        };
}

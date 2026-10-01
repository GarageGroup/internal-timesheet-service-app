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
                MapPreparedDeleteAction(result.PreparedDeleteAction),
                MapPreparedUpdateAction(result.PreparedUpdateAction)));

    private async ValueTask<Result<AgentMessageOut, Failure<AgentMessageSendFailureCode>>> SendMessageAsync(
        AgentUserContext context,
        AgentMessageSendIn input,
        CancellationToken cancellationToken)
    {
        var messageResult = await ResolveMessageAsync(input, cancellationToken).ConfigureAwait(false);
        if (messageResult.IsFailure)
        {
            return messageResult.FailureOrThrow();
        }

        return await InvokeMessageAsync(context, messageResult.SuccessOrThrow(), cancellationToken).ConfigureAwait(false);
    }

    private ValueTask<Result<AgentMessageOut, Failure<AgentMessageSendFailureCode>>> InvokeMessageAsync(
        AgentUserContext context,
        AgentConversationMessageIn message,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            message, cancellationToken)
        .PipeValue(
            (value, token) => messageFunc.InvokeAsync(context, value, token))
        .MapFailure(
            static failure => failure.MapFailureCode(MapMessageFailureCode));

    private ValueTask<Result<AgentConversationMessageIn, Failure<AgentMessageSendFailureCode>>> ResolveMessageAsync(
        AgentMessageSendIn input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Text) is false)
        {
            return ValueTask.FromResult<Result<AgentConversationMessageIn, Failure<AgentMessageSendFailureCode>>>(
                new AgentConversationMessageIn(input.Text, input.Locale));
        }

        return AsyncPipeline.Pipe(
            input.AudioBase64, cancellationToken)
        .Pipe(
            DecodeAudio)
        .MapFailure(
            static failure => failure.MapFailureCode(static _ => AgentMessageSendFailureCode.InvalidAudio))
        .MapSuccess(
            audio => new AgentAudioTranscribeIn(audio, input.AudioMimeType, input.AudioFileName, input.AudioLanguage))
        .ForwardValue(
            TranscribeAudioAsync)
        .MapSuccess(
            transcript => new AgentConversationMessageIn(transcript.Text, input.Locale));
    }

    private ValueTask<Result<AgentAudioTranscribeOut, Failure<AgentMessageSendFailureCode>>> TranscribeAudioAsync(
        AgentAudioTranscribeIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeValue(
            audioTranscribeFunc.InvokeAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(MapAudioFailureCode));

    private static Result<ReadOnlyMemory<byte>, Failure<Unit>> DecodeAudio(string audioBase64)
    {
        if (string.IsNullOrWhiteSpace(audioBase64))
        {
            return Failure.Create("Message text or audio must be specified");
        }

        var buffer = new byte[(audioBase64.Length * 3 + 3) / 4];
        if (Convert.TryFromBase64String(audioBase64, buffer, out var bytesWritten) is false)
        {
            return Failure.Create("Audio must be a valid Base64 string");
        }

        return new ReadOnlyMemory<byte>(buffer, 0, bytesWritten);
    }

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

    private static AgentMessageSendUpdateActionOut? MapPreparedUpdateAction(AgentTimesheetUpdatePrepareOut? action)
        =>
        action is null
            ? null
            : new()
            {
                ActionId = action.ActionId,
                TimesheetId = action.TimesheetId,
                Date = action.Date,
                ProjectId = action.ProjectId,
                ProjectName = action.ProjectName,
                ProjectType = (int)action.ProjectType,
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

    private static AgentMessageSendFailureCode MapAudioFailureCode(AgentAudioTranscribeFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentAudioTranscribeFailureCode.InvalidAudio => AgentMessageSendFailureCode.InvalidAudio,
            AgentAudioTranscribeFailureCode.AudioTooLarge => AgentMessageSendFailureCode.AudioTooLarge,
            AgentAudioTranscribeFailureCode.UnsupportedFormat => AgentMessageSendFailureCode.UnsupportedAudioFormat,
            AgentAudioTranscribeFailureCode.EmptyTranscript => AgentMessageSendFailureCode.EmptyTranscript,
            _ => AgentMessageSendFailureCode.Unknown
        };
}

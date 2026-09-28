using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationMessageFunc
{
    public ValueTask<Result<AgentMessageOut, Failure<AgentConversationMessageFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentConversationMessageIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .PipeValue(
            conversationStore.GetAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(MapStoreFailureCode))
        .ForwardValue(
            (conversation, token) => InvokeMessageAsync(context, input, conversation, token));

    private ValueTask<Result<AgentMessageOut, Failure<AgentConversationMessageFailureCode>>> InvokeMessageAsync(
        AgentUserContext context,
        AgentConversationMessageIn input,
        AgentConversationGetOut conversation,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            new AgentMessageIn(input.Text, input.Locale)
            {
                History = conversation.Messages
            },
            cancellationToken)
        .PipeValue(
            (@in, token) => messageFunc.InvokeAsync(context, @in, token))
        .MapFailure(
            static failure => failure.MapFailureCode(MapMessageFailureCode))
        .ForwardValue(
            (@out, token) => AppendConversationAsync(context, input, conversation.Version, @out, token));

    private ValueTask<Result<AgentMessageOut, Failure<AgentConversationMessageFailureCode>>> AppendConversationAsync(
        AgentUserContext context,
        AgentConversationMessageIn input,
        string? expectedVersion,
        AgentMessageOut output,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .PipeValue(
            (@in, token) => conversationStore.AppendAsync(
                @in,
                expectedVersion,
                new(AgentChatMessageRole.User, input.Text.Trim()),
                new(AgentChatMessageRole.Assistant, output.Text),
                token))
        .MapFailure(
            static failure => failure.MapFailureCode(MapStoreFailureCode))
        .MapSuccess(
            _ => output);

    private static AgentConversationMessageFailureCode MapMessageFailureCode(AgentMessageFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentMessageFailureCode.InvalidMessage => AgentConversationMessageFailureCode.InvalidMessage,
            AgentMessageFailureCode.EmptyResponse => AgentConversationMessageFailureCode.EmptyResponse,
            _ => AgentConversationMessageFailureCode.Unknown
        };

    private static AgentConversationMessageFailureCode MapStoreFailureCode(AgentConversationStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentConversationStoreFailureCode.Conflict => AgentConversationMessageFailureCode.ConversationConflict,
            _ => AgentConversationMessageFailureCode.Unknown
        };
}

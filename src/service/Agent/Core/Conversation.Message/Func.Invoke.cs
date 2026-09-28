using System;
using System.Linq;
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
            (@out, token) => SaveConversationAsync(
                context,
                conversation.Version,
                conversation.Messages,
                input,
                @out,
                token));

    private ValueTask<Result<AgentMessageOut, Failure<AgentConversationMessageFailureCode>>> SaveConversationAsync(
        AgentUserContext context,
        string? expectedVersion,
        FlatArray<AgentChatMessage> conversationMessages,
        AgentConversationMessageIn input,
        AgentMessageOut output,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .PipeValue(
            (@in, token) => conversationStore.SaveAsync(
                @in,
                expectedVersion,
                conversationMessages.AsEnumerable()
                    .Append(new(AgentChatMessageRole.User, input.Text.Trim()))
                    .Append(new(AgentChatMessageRole.Assistant, output.Text))
                    .TakeLast(option.MaxMessageCount)
                    .ToArray(),
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

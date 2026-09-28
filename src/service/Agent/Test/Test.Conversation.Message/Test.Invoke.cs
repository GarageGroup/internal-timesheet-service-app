using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentConversationMessageFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ConversationGetIsFailure_ExpectMappedFailure()
    {
        var messageFunc = new Mock<IAgentMessageFunc>();
        var conversationStore = new Mock<IAgentConversationStore>();
        _ = conversationStore.Setup(f => f.GetAsync(SomeContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentConversationStoreFailureCode.Unknown, "Some error"));

        var actual = await CreateFunc(messageFunc, conversationStore).InvokeAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentConversationMessageFailureCode.Unknown, actual.FailureOrThrow().FailureCode);
        conversationStore.VerifyAll();
        messageFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_MessageIsSuccess_ExpectHistoryAndVersionedAppend()
    {
        var sourceHistory = new AgentChatMessage(AgentChatMessageRole.Assistant, "Предыдущий ответ");
        var conversation = new AgentConversationGetOut([sourceHistory], "version-1");
        var output = new AgentMessageOut("Текущий ответ");
        AgentChatMessage[] expectedMessages =
        [
            sourceHistory,
            new(AgentChatMessageRole.User, "Покажи списания"),
            new(AgentChatMessageRole.Assistant, "Текущий ответ")
        ];
        AgentMessageIn? actualMessageInput = null;

        var messageFunc = new Mock<IAgentMessageFunc>();
        _ = messageFunc.Setup(f => f.InvokeAsync(SomeContext, It.IsAny<AgentMessageIn>(), It.IsAny<CancellationToken>()))
            .Callback<AgentUserContext, AgentMessageIn, CancellationToken>((_, input, _) => actualMessageInput = input)
            .ReturnsAsync(output);

        var conversationStore = new Mock<IAgentConversationStore>();
        _ = conversationStore.Setup(f => f.GetAsync(SomeContext, It.IsAny<CancellationToken>())).ReturnsAsync(conversation);
        _ = conversationStore.Setup(
            f => f.SaveAsync(
                SomeContext,
                "version-1",
                It.Is<FlatArray<AgentChatMessage>>(messages =>
                    messages.AsEnumerable().SequenceEqual(expectedMessages)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actual = await CreateFunc(messageFunc, conversationStore).InvokeAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(output, actual.SuccessOrThrow());
        Assert.NotNull(actualMessageInput);
        Assert.Equal(sourceHistory, Assert.Single(actualMessageInput.History.AsEnumerable()));
        messageFunc.VerifyAll();
        conversationStore.VerifyAll();
    }

    [Fact]
    public static async Task InvokeAsync_AppendIsConflict_ExpectConversationConflictFailure()
    {
        var messageFunc = new Mock<IAgentMessageFunc>();
        _ = messageFunc.Setup(f => f.InvokeAsync(SomeContext, It.IsAny<AgentMessageIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentMessageOut("Some response"));

        var conversationStore = new Mock<IAgentConversationStore>();
        _ = conversationStore.Setup(f => f.GetAsync(SomeContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConversationGetOut(default, "version-1"));
        _ = conversationStore.Setup(
            f => f.SaveAsync(
                SomeContext,
                "version-1",
                It.IsAny<FlatArray<AgentChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentConversationStoreFailureCode.Conflict, "Version conflict"));

        var actual = await CreateFunc(messageFunc, conversationStore).InvokeAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentConversationMessageFailureCode.ConversationConflict, actual.FailureOrThrow().FailureCode);
        messageFunc.VerifyAll();
        conversationStore.VerifyAll();
    }
}

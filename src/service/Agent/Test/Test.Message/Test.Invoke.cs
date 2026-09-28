using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentMessageFuncTest
{
    [Fact]
    public static async Task InvokeAsync_TextIsEmpty_ExpectInvalidMessageFailure()
    {
        var kernelFactory = new Mock<IAgentKernelFactory>();
        var func = CreateFunc(Mock.Of<IChatCompletionService>(), kernelFactory);

        var actual = await func.InvokeAsync(SomeContext, new("  ", "ru"), TestContext.Current.CancellationToken);

        Assert.Equal(AgentMessageFailureCode.InvalidMessage, actual.FailureOrThrow().FailureCode);
        kernelFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ResponseIsSuccessful_ExpectPromptAndResult()
    {
        ChatHistory? actualHistory = null;
        PromptExecutionSettings? actualSettings = null;
        var chatService = new Mock<IChatCompletionService>();

        _ = chatService.Setup(
            f => f.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
        .Callback<ChatHistory, PromptExecutionSettings?, Kernel?, CancellationToken>(
            (history, settings, _, _) =>
            {
                actualHistory = history;
                actualSettings = settings;
            })
        .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, "  Ответ агента  ")]);

        var func = CreateFunc(chatService.Object);
        var actual = await func.InvokeAsync(
            SomeContext,
            new("  Покажи списания за сегодня  ", "ru")
            {
                History =
                [
                    new(AgentChatMessageRole.User, "Покажи проекты"),
                    new(AgentChatMessageRole.Assistant, "Какой проект вас интересует?")
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Ответ агента", actual.SuccessOrThrow().Text);
        Assert.NotNull(actualHistory);
        Assert.Contains("2026-09-28", actualHistory[0].Content);
        Assert.Contains("Europe/Moscow", actualHistory[0].Content);
        Assert.Equal(AuthorRole.User, actualHistory[1].Role);
        Assert.Equal("Покажи проекты", actualHistory[1].Content);
        Assert.Equal(AuthorRole.Assistant, actualHistory[2].Role);
        Assert.Equal("Какой проект вас интересует?", actualHistory[2].Content);
        Assert.Equal("Покажи списания за сегодня", actualHistory.Last().Content);
        Assert.NotNull(actualSettings?.FunctionChoiceBehavior);
        chatService.VerifyAll();
    }

    [Fact]
    public static async Task InvokeAsync_HistoryIsTooLong_ExpectInvalidMessageFailure()
    {
        var kernelFactory = new Mock<IAgentKernelFactory>();
        var func = CreateFunc(Mock.Of<IChatCompletionService>(), kernelFactory);
        var input = new AgentMessageIn("Some text", "ru")
        {
            History = System.Linq.Enumerable.Range(0, 21)
                .Select(static i => new AgentChatMessage(AgentChatMessageRole.User, $"Message {i}"))
                .ToArray()
        };

        var actual = await func.InvokeAsync(SomeContext, input, TestContext.Current.CancellationToken);

        Assert.Equal(AgentMessageFailureCode.InvalidMessage, actual.FailureOrThrow().FailureCode);
        kernelFactory.VerifyNoOtherCalls();
    }
}

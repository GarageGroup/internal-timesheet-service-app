using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Message.Send.Test;

partial class AgentMessageSendFuncTest
{
    [Theory]
    [InlineData(AgentUserContextResolveFailureCode.InvalidIdentity, AgentMessageSendFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UnsupportedChat, AgentMessageSendFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UserNotLinked, AgentMessageSendFailureCode.UserNotLinked)]
    [InlineData(AgentUserContextResolveFailureCode.AmbiguousBinding, AgentMessageSendFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.BindingSignedOut, AgentMessageSendFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.UserDisabled, AgentMessageSendFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.MissingEntraObjectId, AgentMessageSendFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.Unknown, AgentMessageSendFailureCode.Unknown)]
    public static async Task InvokeAsync_UserResolveFailure_ExpectMappedFailure(
        AgentUserContextResolveFailureCode sourceCode,
        AgentMessageSendFailureCode expectedCode)
    {
        var sourceException = new Exception("Some error");
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(sourceCode, "Resolve failed", sourceException));
        var messageFunc = new Mock<IAgentConversationMessageFunc>(MockBehavior.Strict);

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(expectedCode, failure.FailureCode);
        Assert.Same(sourceException, failure.SourceException);
    }

    [Theory]
    [InlineData(AgentConversationMessageFailureCode.InvalidMessage, AgentMessageSendFailureCode.InvalidMessage)]
    [InlineData(AgentConversationMessageFailureCode.EmptyResponse, AgentMessageSendFailureCode.EmptyResponse)]
    [InlineData(AgentConversationMessageFailureCode.ConversationConflict, AgentMessageSendFailureCode.ConversationConflict)]
    [InlineData(AgentConversationMessageFailureCode.Unknown, AgentMessageSendFailureCode.Unknown)]
    public static async Task InvokeAsync_MessageFailure_ExpectMappedFailure(
        AgentConversationMessageFailureCode sourceCode,
        AgentMessageSendFailureCode expectedCode)
    {
        var resolver = BuildResolver();
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(Failure.Create(sourceCode, "Message failed"));

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_UserResolved_ExpectTrustedContextAndMessagePassed()
    {
        AgentUserIdentity? actualIdentity = null;
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<AgentUserIdentity, CancellationToken>((identity, _) => actualIdentity = identity)
            .ReturnsAsync(SomeContext);
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("Some response"));

        _ = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(new AgentUserIdentity(101, 202, 202), actualIdentity);
        messageFunc.Verify(f => f.InvokeAsync(
            SomeContext,
            new AgentConversationMessageIn("Some question", "ru"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_AllCallsSuccessful_ExpectResponse()
    {
        var resolver = BuildResolver();
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("Some response"));

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(new AgentMessageSendOut("Some response"), actual.SuccessOrThrow());
    }

    [Fact]
    public static async Task InvokeAsync_ActionWasPrepared_ExpectStructuredPreview()
    {
        var resolver = BuildResolver();
        var preparedAction = new AgentTimesheetCreatePrepareOut(
            new("84e6c5b8-1597-4a2e-821a-f392c8c0ae3d"),
            new(2026, 09, 29),
            new("d9cb8306-dd0c-499b-ad90-44b9a324e30c"),
            "Some project",
            ProjectType.Project,
            1.5m,
            "Some description",
            new(2026, 09, 29, 12, 10, 00, TimeSpan.Zero));
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("Confirm action", preparedAction));

        var actual = (await new AgentMessageSendFunc(resolver.Object, messageFunc.Object).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var expectedAction = new AgentMessageSendActionOut
        {
            ActionId = preparedAction.ActionId,
            Date = preparedAction.Date,
            ProjectId = preparedAction.ProjectId,
            ProjectName = preparedAction.ProjectName,
            ProjectType = (int)preparedAction.ProjectType,
            Duration = preparedAction.Duration,
            Description = preparedAction.Description,
            ExpiresAt = preparedAction.ExpiresAt
        };
        Assert.Equal(new AgentMessageSendOut("Confirm action", expectedAction), actual);
    }
}

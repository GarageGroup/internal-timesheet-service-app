using System;
using System.Linq;
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

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
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

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
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

        _ = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
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

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
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

        var actual = (await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var expectedAction = new AgentMessageSendCreateActionOut
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

    [Fact]
    public static async Task InvokeAsync_DeleteActionWasPrepared_ExpectStructuredPreview()
    {
        var resolver = BuildResolver();
        var preparedAction = new AgentTimesheetDeletePrepareOut(
            new("2c596490-1917-428c-88b9-3e8dc55f835a"),
            new("46606dc6-335f-4271-86b7-ff9540e9f480"),
            new(2026, 09, 30),
            "Another project",
            2.25m,
            "Another description",
            new(2026, 09, 30, 13, 10, 00, TimeSpan.Zero));
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("Confirm delete", PreparedDeleteAction: preparedAction));

        var actual = (await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var expectedAction = new AgentMessageSendDeleteActionOut
        {
            ActionId = preparedAction.ActionId,
            TimesheetId = preparedAction.TimesheetId,
            Date = preparedAction.Date,
            ProjectName = preparedAction.ProjectName,
            Duration = preparedAction.Duration,
            Description = preparedAction.Description,
            ExpiresAt = preparedAction.ExpiresAt
        };
        Assert.Equal(new AgentMessageSendOut("Confirm delete", preparedDeleteAction: expectedAction), actual);
    }

    [Fact]
    public static async Task InvokeAsync_UpdateActionWasPrepared_ExpectStructuredPreview()
    {
        var resolver = BuildResolver();
        var preparedAction = new AgentTimesheetUpdatePrepareOut(
            new("da7d99f3-939c-4842-94f0-329157a061c8"),
            new("53478fc6-5d80-4148-9b93-f6b65457cf6d"),
            new(2026, 09, 28),
            new("7712b133-f72f-4b52-a0cc-78d0882ba84f"),
            "Updated project",
            ProjectType.Incident,
            3.75m,
            "Updated description",
            new(2026, 09, 30, 14, 10, 00, TimeSpan.Zero));
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentConversationMessageIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("Confirm update", PreparedUpdateAction: preparedAction));

        var actual = (await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, AudioTranscribeFunc).InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var expectedAction = new AgentMessageSendUpdateActionOut
        {
            ActionId = preparedAction.ActionId,
            TimesheetId = preparedAction.TimesheetId,
            Date = preparedAction.Date,
            ProjectId = preparedAction.ProjectId,
            ProjectName = preparedAction.ProjectName,
            ProjectType = (int)preparedAction.ProjectType,
            Duration = preparedAction.Duration,
            Description = preparedAction.Description,
            ExpiresAt = preparedAction.ExpiresAt
        };
        Assert.Equal(new AgentMessageSendOut("Confirm update", preparedUpdateAction: expectedAction), actual);
    }

    [Fact]
    public static async Task InvokeAsync_VoiceMessage_ExpectTranscribedMessageSent()
    {
        var input = new AgentMessageSendIn(
            101,
            303,
            202,
            202,
            string.Empty,
            "ru",
            Convert.ToBase64String([1, 2, 3]),
            "audio/ogg",
            "voice.ogg",
            "ru");
        var resolver = BuildResolver();
        var audioFunc = new Mock<IAgentAudioTranscribeFunc>();
        _ = audioFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentAudioTranscribeIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentAudioTranscribeOut("Сколько часов я списал сегодня?"));
        var messageFunc = new Mock<IAgentConversationMessageFunc>();
        _ = messageFunc.Setup(static f => f.InvokeAsync(
            SomeContext,
            new AgentConversationMessageIn("Сколько часов я списал сегодня?", "ru"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AgentMessageOut("За сегодня списано 8 часов"));

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, audioFunc.Object).InvokeAsync(
            input,
            TestContext.Current.CancellationToken);

        Assert.Equal(new AgentMessageSendOut("За сегодня списано 8 часов"), actual.SuccessOrThrow());
        audioFunc.Verify(static f => f.InvokeAsync(
            It.Is<AgentAudioTranscribeIn>(static input =>
                input.Audio.ToArray().SequenceEqual(new byte[] { 1, 2, 3 }) &&
                input.MimeType == "audio/ogg" &&
                input.FileName == "voice.ogg" &&
                input.Language == "ru"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_InvalidAudioBase64_ExpectInvalidAudioFailure()
    {
        var input = new AgentMessageSendIn(101, 303, 202, 202, string.Empty, "ru", "not-base64", "audio/ogg", "voice.ogg", "ru");
        var resolver = BuildResolver();
        var messageFunc = new Mock<IAgentConversationMessageFunc>(MockBehavior.Strict);
        var audioFunc = new Mock<IAgentAudioTranscribeFunc>(MockBehavior.Strict);

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, audioFunc.Object).InvokeAsync(
            input,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentMessageSendFailureCode.InvalidAudio, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData(AgentAudioTranscribeFailureCode.InvalidAudio, AgentMessageSendFailureCode.InvalidAudio)]
    [InlineData(AgentAudioTranscribeFailureCode.AudioTooLarge, AgentMessageSendFailureCode.AudioTooLarge)]
    [InlineData(AgentAudioTranscribeFailureCode.UnsupportedFormat, AgentMessageSendFailureCode.UnsupportedAudioFormat)]
    [InlineData(AgentAudioTranscribeFailureCode.EmptyTranscript, AgentMessageSendFailureCode.EmptyTranscript)]
    [InlineData(AgentAudioTranscribeFailureCode.Unknown, AgentMessageSendFailureCode.Unknown)]
    public static async Task InvokeAsync_AudioFailure_ExpectMappedFailure(
        AgentAudioTranscribeFailureCode sourceCode,
        AgentMessageSendFailureCode expectedCode)
    {
        var input = new AgentMessageSendIn(
            101,
            303,
            202,
            202,
            string.Empty,
            "ru",
            Convert.ToBase64String([1]),
            "audio/ogg",
            "voice.ogg",
            "ru");
        var resolver = BuildResolver();
        var messageFunc = new Mock<IAgentConversationMessageFunc>(MockBehavior.Strict);
        var audioFunc = new Mock<IAgentAudioTranscribeFunc>();
        _ = audioFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentAudioTranscribeIn>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(Failure.Create(sourceCode, "Audio failed"));

        var actual = await new AgentMessageSendFunc(resolver.Object, messageFunc.Object, audioFunc.Object).InvokeAsync(
            input,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }
}

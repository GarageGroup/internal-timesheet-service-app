using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Audio.Test;

partial class AgentAudioTranscribeFuncTest
{
    [Fact]
    public static async Task InvokeAsync_AudioDisabled_ExpectInvalidAudioFailure()
    {
        var service = new Mock<IAudioToTextService>(MockBehavior.Strict);
        var actual = await CreateFunc(service, enabled: false).InvokeAsync(
            new(new byte[] { 1 }, "audio/ogg", "voice.ogg", "ru"),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentAudioTranscribeFailureCode.InvalidAudio, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData("", "voice.ogg", AgentAudioTranscribeFailureCode.InvalidAudio)]
    [InlineData("audio/wav", "voice.wav", AgentAudioTranscribeFailureCode.UnsupportedFormat)]
    public static async Task InvokeAsync_InvalidInput_ExpectFailure(
        string mimeType,
        string fileName,
        AgentAudioTranscribeFailureCode expectedCode)
    {
        var service = new Mock<IAudioToTextService>(MockBehavior.Strict);
        var actual = await CreateFunc(service).InvokeAsync(
            new(new byte[] { 1 }, mimeType, fileName, "ru"),
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_AudioTooLarge_ExpectFailure()
    {
        var service = new Mock<IAudioToTextService>(MockBehavior.Strict);
        var actual = await CreateFunc(service, 1).InvokeAsync(
            new(new byte[] { 1, 2 }, "audio/ogg", "voice.ogg", "ru"),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentAudioTranscribeFailureCode.AudioTooLarge, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_ValidAudio_ExpectTrimmedTranscript()
    {
        var service = new Mock<IAudioToTextService>();
        _ = service.Setup(static s => s.GetTextContentsAsync(
            It.IsAny<AudioContent>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<TextContent> { new("  Спиши полчаса  ") });

        var actual = await CreateFunc(service).InvokeAsync(
            new(new byte[] { 1, 2 }, "audio/ogg", "voice.ogg", "ru"),
            TestContext.Current.CancellationToken);

        Assert.Equal("Спиши полчаса", actual.SuccessOrThrow().Text);
    }
}

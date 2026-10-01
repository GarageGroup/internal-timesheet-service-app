using Microsoft.SemanticKernel.AudioToText;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Audio.Test;

public static partial class AgentAudioTranscribeFuncTest
{
    private static AgentAudioTranscribeFunc CreateFunc(
        Mock<IAudioToTextService> service,
        int maxFileSizeBytes = 100,
        bool enabled = true)
        =>
        new(
            service.Object,
            new AgentAudioTranscribeOption(maxFileSizeBytes, ["audio/ogg"])
            {
                Enabled = enabled
            });
}

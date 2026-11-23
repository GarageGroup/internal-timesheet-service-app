using Microsoft.SemanticKernel.AudioToText;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentAudioTranscribeFunc(
    IAudioToTextService audioToTextService,
    AgentAudioTranscribeOption option) : IAgentAudioTranscribeFunc;

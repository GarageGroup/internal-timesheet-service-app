using System;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.AudioToText;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Audio.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentAudioTranscribeDependency
{
    public static Dependency<IAgentAudioTranscribeFunc> UseAgentAudioTranscribeFunc(
        this Dependency<IAudioToTextService, AgentAudioTranscribeOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentAudioTranscribeFunc>(CreateFunc);

        static AgentAudioTranscribeFunc CreateFunc(IAudioToTextService service, AgentAudioTranscribeOption option)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(option);

            return new(service, option);
        }
    }
}

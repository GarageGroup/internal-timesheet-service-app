using System;
using System.Runtime.CompilerServices;
using Azure.Core;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.AudioToText;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Audio.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentAudioTranscribeDependency
{
    public static Dependency<IAudioToTextService> UseAzureOpenAIAudioToTextService(
        this Dependency<TokenCredential, AgentAudioProviderOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAudioToTextService>(CreateService);

        static AzureOpenAIAudioToTextService CreateService(TokenCredential credential, AgentAudioProviderOption option)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(option);

            return new(option.DeploymentName, option.Endpoint.AbsoluteUri, credential, option.ModelId);
        }
    }

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

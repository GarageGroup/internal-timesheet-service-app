using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentAudioTranscribeFunc
{
    ValueTask<Result<AgentAudioTranscribeOut, Failure<AgentAudioTranscribeFailureCode>>> InvokeAsync(
        AgentAudioTranscribeIn input,
        CancellationToken cancellationToken);
}

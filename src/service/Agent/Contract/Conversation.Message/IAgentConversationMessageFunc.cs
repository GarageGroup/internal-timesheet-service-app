using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentConversationMessageFunc
{
    ValueTask<Result<AgentMessageOut, Failure<AgentConversationMessageFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentConversationMessageIn input,
        CancellationToken cancellationToken);
}

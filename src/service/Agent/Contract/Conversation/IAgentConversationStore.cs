using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentConversationStore
{
    ValueTask<Result<AgentConversationGetOut, Failure<AgentConversationStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, Failure<AgentConversationStoreFailureCode>>> AppendAsync(
        AgentUserContext context,
        string? expectedVersion,
        AgentChatMessage userMessage,
        AgentChatMessage assistantMessage,
        CancellationToken cancellationToken);
}

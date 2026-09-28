using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentConversationStore
{
    ValueTask<Result<AgentConversationGetOut, Failure<AgentConversationStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, Failure<AgentConversationStoreFailureCode>>> SaveAsync(
        AgentUserContext context,
        string? expectedVersion,
        FlatArray<AgentChatMessage> messages,
        CancellationToken cancellationToken);
}

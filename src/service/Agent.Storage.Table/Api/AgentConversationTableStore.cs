using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentConversationTableStore(TableClient tableClient) : IAgentConversationStore;

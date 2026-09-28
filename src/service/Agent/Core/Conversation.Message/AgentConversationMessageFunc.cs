namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentConversationMessageFunc(
    IAgentMessageFunc messageFunc,
    IAgentConversationStore conversationStore,
    AgentConversationMessageOption option) : IAgentConversationMessageFunc;

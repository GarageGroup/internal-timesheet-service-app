namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentMessageSendFunc(
    IAgentUserContextResolver userContextResolver,
    IAgentConversationMessageFunc messageFunc) : IAgentMessageSendFunc;

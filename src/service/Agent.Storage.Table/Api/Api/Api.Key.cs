using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationTableApi
{
    private static string GetPartitionKey(AgentUserContext context)
        =>
        context.BotId.ToString(CultureInfo.InvariantCulture);

    private static string GetRowKey(AgentUserContext context)
        =>
        $"{context.TelegramUserId.ToString(CultureInfo.InvariantCulture)}-{context.TelegramChatId.ToString(CultureInfo.InvariantCulture)}";
}

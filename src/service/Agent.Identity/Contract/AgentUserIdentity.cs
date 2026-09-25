namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentUserIdentity
{
    public AgentUserIdentity(long botId, long telegramUserId, long telegramChatId)
    {
        BotId = botId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
    }

    public long BotId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }
}

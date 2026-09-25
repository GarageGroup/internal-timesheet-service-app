using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentProfileGetIn
{
    public AgentProfileGetIn(
        [ClaimIn("timesheet_bot_id")] long botId,
        [JsonBodyIn] long telegramUserId,
        [JsonBodyIn] long telegramChatId)
    {
        BotId = botId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
    }

    public long BotId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }
}

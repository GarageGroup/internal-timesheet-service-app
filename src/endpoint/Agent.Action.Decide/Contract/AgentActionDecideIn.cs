using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentActionDecideIn
{
    public AgentActionDecideIn(
        [ClaimIn("timesheet_bot_id")] long botId,
        [RouteIn] Guid actionId,
        [JsonBodyIn] long telegramUpdateId,
        [JsonBodyIn] long telegramUserId,
        [JsonBodyIn] long telegramChatId,
        [JsonBodyIn] AgentActionDecision decision)
    {
        BotId = botId;
        ActionId = actionId;
        TelegramUpdateId = telegramUpdateId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
        Decision = decision;
    }

    public long BotId { get; }

    public Guid ActionId { get; }

    public long TelegramUpdateId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }

    public AgentActionDecision Decision { get; }
}

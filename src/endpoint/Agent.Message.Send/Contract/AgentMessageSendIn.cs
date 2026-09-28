using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageSendIn
{
    public AgentMessageSendIn(
        [ClaimIn("timesheet_bot_id")] long botId,
        [JsonBodyIn] long telegramUpdateId,
        [JsonBodyIn] long telegramUserId,
        [JsonBodyIn] long telegramChatId,
        [JsonBodyIn] [AllowNull] string text,
        [JsonBodyIn] string? locale)
    {
        BotId = botId;
        TelegramUpdateId = telegramUpdateId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
        Text = text.OrEmpty();
        Locale = locale;
    }

    public long BotId { get; }

    public long TelegramUpdateId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }

    public string Text { get; }

    public string? Locale { get; }
}

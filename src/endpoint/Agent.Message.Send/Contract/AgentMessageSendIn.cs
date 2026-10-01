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
        [JsonBodyIn] string? locale,
        [JsonBodyIn] [AllowNull] string audioBase64 = null,
        [JsonBodyIn] [AllowNull] string audioMimeType = null,
        [JsonBodyIn] [AllowNull] string audioFileName = null,
        [JsonBodyIn] [AllowNull] string audioLanguage = null)
    {
        BotId = botId;
        TelegramUpdateId = telegramUpdateId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
        Text = text.OrEmpty();
        Locale = locale;
        AudioBase64 = audioBase64.OrEmpty();
        AudioMimeType = audioMimeType.OrEmpty();
        AudioFileName = audioFileName.OrEmpty();
        AudioLanguage = audioLanguage.OrEmpty();
    }

    public long BotId { get; }

    public long TelegramUpdateId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }

    public string Text { get; }

    public string? Locale { get; }

    public string AudioBase64 { get; }

    public string AudioMimeType { get; }

    public string AudioFileName { get; }

    public string AudioLanguage { get; }
}

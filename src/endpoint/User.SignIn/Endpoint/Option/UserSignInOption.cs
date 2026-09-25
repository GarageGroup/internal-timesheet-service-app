using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class UserSignInOption
{
    public UserSignInOption(
        string botToken,
        TimeSpan? telegramDataMaxAge = null,
        TimeSpan? clockSkew = null)
    {
        BotToken = botToken.OrEmpty();

        TelegramDataMaxAge = telegramDataMaxAge ?? TimeSpan.FromMinutes(5);
        if (TelegramDataMaxAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramDataMaxAge), "Telegram data max age must be positive");
        }

        ClockSkew = clockSkew ?? TimeSpan.FromSeconds(30);
        if (ClockSkew < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(clockSkew), "Clock skew cannot be negative");
        }
    }

    public string BotToken { get; }

    public TimeSpan TelegramDataMaxAge { get; }

    public TimeSpan ClockSkew { get; }
}

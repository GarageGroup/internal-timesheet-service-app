using GarageGroup.Infra;
using PrimeFuncPack;
using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<UserSignInEndpoint> UseUserSignInEndpoint()
        =>
        Pipeline.Pipe(
            UseDataverseApi())
        .With(
            UseBotApi())
        .With(
            ResolveUserSignInOption)
        .UseUserSignInEndpoint();

    private static UserSignInOption ResolveUserSignInOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();

        return new(
            botToken: configuration.GetBotTokenOrThrow(),
            telegramDataMaxAge: TimeSpan.FromMinutes(
                GetConfiguredNumber(configuration["TelegramBot:WebAppDataMaxAgeMinutes"], 5, false)),
            clockSkew: TimeSpan.FromSeconds(
                GetConfiguredNumber(configuration["TelegramBot:WebAppDataClockSkewSeconds"], 30, true)));
    }

    private static int GetConfiguredNumber(string? source, int defaultValue, bool allowZero)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return defaultValue;
        }

        if (int.TryParse(source, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
            (value > 0 || allowZero && value is 0))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration value '{source}' is invalid");
    }
}

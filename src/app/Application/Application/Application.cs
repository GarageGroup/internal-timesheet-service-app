using GarageGroup.Infra;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class Application
{
    private static Dependency<IDataverseApiClient> UseDataverseApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("DataverseApi")
        .UseTokenCredentialStandard()
        .UsePollyStandard()
        .UseDataverseApiClient("Dataverse");

    private static Dependency<ISqlApi> UseSqlApi()
        =>
        DataverseDbProvider.Configure("Dataverse").UseSqlApi();

    private static Dependency<IBotApi> UseBotApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(
            "BotApi")
        .UsePollyStandard()
        .UseHttpApi(
            ResolveHttpBotApiOption)
        .UseTelegramBotApi()
        .UseBotApi();

    private static HttpApiOption ResolveHttpBotApiOption(IServiceProvider serviceProvider)
        =>
        new()
        {
            BaseAddress = new($"https://api.telegram.org/bot{serviceProvider.GetConfiguration().GetBotTokenOrThrow()}/")
        };

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();

    private static string GetBotTokenOrThrow(this IConfiguration configuration)
    {
        var token = configuration["TelegramBot:Token"];

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Bot token must be specified");
        }

        return token;
    }
}
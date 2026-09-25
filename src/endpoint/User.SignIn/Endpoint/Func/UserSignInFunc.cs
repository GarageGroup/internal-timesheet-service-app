using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class UserSignInFunc : IUserSignInFunc
{
    private readonly IDataverseApiClient dataverseApi;

    private readonly IBotInfoGetSupplier botApi;

    private readonly ITelegramWebAppDataValidator telegramDataValidator;

    internal UserSignInFunc(
        IDataverseApiClient dataverseApi,
        IBotInfoGetSupplier botApi,
        ITelegramWebAppDataValidator telegramDataValidator)
    {
        this.dataverseApi = dataverseApi;
        this.botApi = botApi;
        this.telegramDataValidator = telegramDataValidator;
    }

    private static UserSignInFailureCode ToUserSignInFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => UserSignInFailureCode.SystemUserNotFound,
            _ => default
        };

    private sealed record class UserChatId
    {
        public required Guid SystemUserId { get; init; }

        public required long ChatId { get; init; }
    }

    private sealed record class SignInContext
    {
        public required UserChatId User { get; init; }

        public required SystemUserJson SystemUser { get; init; }

        public required BotInfoGetOut Bot { get; init; }
    }
}

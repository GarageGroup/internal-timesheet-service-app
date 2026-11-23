using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test;

public static partial class UserSignInFuncTest
{
    private static readonly UserSignInIn SomeInput
        =
        new(
            systemUserId: new("c22c378d-7913-4316-8e61-5a5c35987355"),
            telegramData: "query_id=AAGmGqACAASCAKYaoAKgWTfQ&user=%7B%22id%22%3A123123%2C%22" +
                "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
                "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
                "hash=2fa9c34a28f2a843eca1a086262000e6d0bda91db3a8ddf4002ca5bd26a5c224");

    private static readonly DataverseEntityGetOut<SystemUserJson> SomeSystemUserResult
        =
        new(
            value: new()
            {
                FullName = "Some user name"
            });

    private static readonly BotInfoGetOut SomeBotInfo
        =
        new(79237382, "SomeBotName");

    private static Mock<IDataverseApiClient> BuildMockDataverseApi(
        in Result<DataverseEntityGetOut<SystemUserJson>, Failure<DataverseFailureCode>> getResult,
        in Result<Unit, Failure<DataverseFailureCode>> upsertResult)
    {
        var mock = new Mock<IDataverseApiClient>();

        _ = mock.Setup(
            static a => a.GetEntityAsync<SystemUserJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(getResult);

        _ = mock.Setup(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<UserJson>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(upsertResult);

        _ = mock.Setup(
            static a => a.GetEntitySetAsync<UserBindingJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new DataverseEntitySetGetOut<UserBindingJson>(default));

        return mock;
    }

    private static void SetupBindings(
        Mock<IDataverseApiClient> mock,
        in Result<DataverseEntitySetGetOut<UserBindingJson>, Failure<DataverseFailureCode>> result)
        =>
        mock.Setup(
            static a => a.GetEntitySetAsync<UserBindingJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(result);

    private static Mock<IBotInfoGetSupplier> BuildMockBotApi(
        in Result<BotInfoGetOut, Failure<Unit>> result)
    {
        var mock = new Mock<IBotInfoGetSupplier>();

        _ = mock.Setup(
            static a => a.GetBotInfoAsync(It.IsAny<Unit>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(result);

        return mock;
    }

    private static Mock<ITelegramWebAppDataValidator> BuildMockTelegramDataValidator(
        in Result<long, Failure<UserSignInFailureCode>> result)
    {
        var mock = new Mock<ITelegramWebAppDataValidator>();
        _ = mock.Setup(static a => a.Validate(It.IsAny<string>())).Returns(result);
        return mock;
    }
}

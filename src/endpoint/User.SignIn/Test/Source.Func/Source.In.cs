using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test;

using DataverseUserOut = DataverseEntityGetOut<SystemUserJson>;
using DataverseUserIn = DataverseEntityUpdateIn<UserJson>;

partial class UserSignInFuncSource
{
    public static TheoryData<BotInfoGetOut, UserSignInIn, DataverseUserOut, DataverseUserIn> InputTestData
        =>
        new()
        {
            {
                new(
                    id: 111222,
                    username: "Some bot name"),
                new(
                    systemUserId: new("2e159d1a-42ac-4c9f-af70-b094ba32a786"),
                    telegramData: "query_id=AAGmGqACAASCAKYaoAKgWTfQ&user=%7B%22id%22%3A123123%2C%22" +
                        "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
                        "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
                        "hash=2fa9c34a28f2a843eca1a086262000e6d0bda91db3a8ddf4002ca5bd26a5c224"),
                new(
                    value: new()
                    {
                        FullName = "Some user name"
                    }),
                new(
                    entityPluralName: "gg_telegram_bot_users",
                    entityKey: new DataverseAlternateKey(
                        idArguments:
                        [
                            new("_gg_systemuser_id_value", "2e159d1a-42ac-4c9f-af70-b094ba32a786"),
                            new("gg_bot_id", "'111222'")
                        ]),
                    entityData: new()
                    {
                        BotId = 111222,
                        BotName = "Some bot name - Some user name",
                        ChatId = 123123,
                        UserLookupValue = $"/systemusers(2e159d1a-42ac-4c9f-af70-b094ba32a786)",
                        IsSignedOut = false,
                    })
                {
                    OperationType = DataverseUpdateOperationType.Upsert
                }
            }
        };
}

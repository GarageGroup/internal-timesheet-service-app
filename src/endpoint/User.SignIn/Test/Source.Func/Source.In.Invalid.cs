using System;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test;

partial class UserSignInFuncSource
{
    public static TheoryData<UserSignInIn, Failure<UserSignInFailureCode>> InputInvalidTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("bdcef53b-609b-475b-9758-58b06d46fdcf"),
                    telegramData: "Invalid data"),
                new(UserSignInFailureCode.InvalidTelegramData, "Invalid telegram data")
            },
            {
                new(
                    systemUserId: new("c22c378d-7913-4316-8e61-5a5c35987355"),
                    telegramData: "query_id=CAAmGqACACAACKYaoCKgUTfQ&user=%7B%22id%22%3A123123%2C%22" +
                        "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
                        "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
                        "hash=a23w23sg12dsd141w15t354w41t24d6g765r16rfgg12345678901234567wqg1h"),
                new(UserSignInFailureCode.InvalidTelegramData, "Invalid hash")
            },
            {
                new(
                    systemUserId: new("c22c378d-7913-4316-8e61-5a5c35987355"),
                    telegramData: "query_id=AAGmGqACAASCAKYaoAKgWTfQ&user=%7B%22id%22%3As%2C%22" +
                        "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
                        "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
                        "hash=2168473c9bcba69307d6efc012f79e48704a44696a48c30ad6be0060acce5c89"),
                new(UserSignInFailureCode.InvalidTelegramData, "Invalid id")
            }
        };
}

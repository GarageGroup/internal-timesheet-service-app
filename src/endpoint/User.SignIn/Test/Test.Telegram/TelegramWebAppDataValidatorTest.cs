using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test;

public static class TelegramWebAppDataValidatorTest
{
    private const string BotToken = "1234567890:QWG2gaQTcv14ttw1wqrEgqw1wQqTQx5QWeR";

    private const long AuthDateSeconds = 1720097842;

    private static readonly DateTimeOffset AuthDate = DateTimeOffset.FromUnixTimeSeconds(AuthDateSeconds);

    private const string KnownTelegramData
        =
        "query_id=AAGmGqACAASCAKYaoAKgWTfQ&user=%7B%22id%22%3A123123%2C%22" +
        "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
        "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
        "hash=2fa9c34a28f2a843eca1a086262000e6d0bda91db3a8ddf4002ca5bd26a5c224";

    [Fact]
    public static void Validate_KnownTelegramData_ExpectUserId()
    {
        var validator = CreateValidator(AuthDate.AddMinutes(4));

        var actual = validator.Validate(KnownTelegramData);

        Assert.Equal(123123L, actual.SuccessOrThrow());
    }

    [Fact]
    public static void Validate_ParametersHaveDifferentOrder_ExpectUserId()
    {
        var telegramData = BuildTelegramData(
            ("user", "{\"first_name\":\"Test User\",\"id\":987654321}"),
            ("query_id", "Some+Query"),
            ("auth_date", AuthDateSeconds.ToString()));

        var actual = CreateValidator(AuthDate).Validate(telegramData);

        Assert.Equal(987654321L, actual.SuccessOrThrow());
    }

    [Fact]
    public static void Validate_ExpiredAuthDate_ExpectFailure()
    {
        var actual = CreateValidator(AuthDate.AddMinutes(5).AddSeconds(31)).Validate(KnownTelegramData);

        AssertInvalid(actual, "Telegram auth_date is missing, expired or invalid");
    }

    [Fact]
    public static void Validate_AuthDateIsInFuture_ExpectFailure()
    {
        var actual = CreateValidator(AuthDate.AddSeconds(-31)).Validate(KnownTelegramData);

        AssertInvalid(actual, "Telegram auth_date is missing, expired or invalid");
    }

    [Fact]
    public static void Validate_MissingAuthDate_ExpectFailure()
    {
        var telegramData = BuildTelegramData(("user", "{\"id\":123}"));

        var actual = CreateValidator(AuthDate).Validate(telegramData);

        AssertInvalid(actual, "Telegram auth_date is missing, expired or invalid");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"id\":0}")]
    [InlineData("{\"id\":-1}")]
    [InlineData("not-json")]
    public static void Validate_InvalidUser_ExpectFailure(string userJson)
    {
        var telegramData = BuildTelegramData(
            ("auth_date", AuthDateSeconds.ToString()),
            ("user", userJson));

        var actual = CreateValidator(AuthDate).Validate(telegramData);

        Assert.Equal(UserSignInFailureCode.InvalidTelegramData, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static void Validate_InvalidHash_ExpectFailure()
    {
        var telegramData = KnownTelegramData[..^1] + "0";

        var actual = CreateValidator(AuthDate).Validate(telegramData);

        AssertInvalid(actual, "Telegram hash is invalid");
    }

    [Fact]
    public static void Validate_DuplicateParameter_ExpectFailure()
    {
        var telegramData = KnownTelegramData + "&user=%7B%22id%22%3A456%7D";

        var actual = CreateValidator(AuthDate).Validate(telegramData);

        AssertInvalid(actual, "Telegram data contains an empty or duplicate parameter");
    }

    [Fact]
    public static void Validate_InvalidPercentEncoding_ExpectFailure()
    {
        var actual = CreateValidator(AuthDate).Validate("user=%ZZ&hash=00");

        AssertInvalid(actual, "Telegram data contains invalid escaping");
    }

    private static TelegramWebAppDataValidator CreateValidator(DateTimeOffset utcNow)
        =>
        new(
            new UserSignInOption(BotToken),
            new TestTimeProvider(utcNow));

    private static string BuildTelegramData(params (string Key, string Value)[] parameters)
    {
        var dataCheckString = string.Join(
            '\n',
            parameters.OrderBy(static item => item.Key, StringComparer.Ordinal).Select(static item => $"{item.Key}={item.Value}"));

        using var secretAlgorithm = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        var secretKey = secretAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(BotToken));

        using var hashAlgorithm = new HMACSHA256(secretKey);
        var hash = Convert.ToHexString(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();

        var encodedParameters = parameters.Select(
            static item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}");

        return $"{string.Join('&', encodedParameters)}&hash={hash}";
    }

    private static void AssertInvalid(
        Result<long, Failure<UserSignInFailureCode>> actual,
        string expectedMessage)
    {
        var failure = actual.FailureOrThrow();
        Assert.Equal(UserSignInFailureCode.InvalidTelegramData, failure.FailureCode);
        Assert.Equal(expectedMessage, failure.FailureMessage);
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            =>
            utcNow;
    }
}

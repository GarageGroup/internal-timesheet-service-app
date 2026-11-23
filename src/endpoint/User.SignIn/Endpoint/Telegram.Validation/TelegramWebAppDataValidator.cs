using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed class TelegramWebAppDataValidator : ITelegramWebAppDataValidator
{
    private const string TelegramWebAppData = "WebAppData";

    private const string HashParameterName = "hash";

    private const string AuthDateParameterName = "auth_date";

    private const string UserParameterName = "user";

    private readonly UserSignInOption option;

    private readonly TimeProvider timeProvider;

    internal TelegramWebAppDataValidator(UserSignInOption option, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.option = option;
        this.timeProvider = timeProvider;
    }

    public Result<long, Failure<UserSignInFailureCode>> Validate(string telegramData)
    {
        var parseResult = ParseParameters(telegramData);
        if (parseResult.IsFailure)
        {
            return parseResult.FailureOrThrow();
        }

        var parameters = parseResult.SuccessOrThrow();
        if (parameters.Remove(HashParameterName, out var hashText) is false || TryDecodeHash(hashText, out var actualHash) is false)
        {
            return Invalid("Telegram hash is missing or invalid");
        }

        var dataCheckString = string.Join(
            '\n',
            parameters.OrderBy(static pair => pair.Key, StringComparer.Ordinal).Select(static pair => $"{pair.Key}={pair.Value}"));

        var expectedHash = ComputeHash(option.BotToken, dataCheckString);
        if (CryptographicOperations.FixedTimeEquals(expectedHash, actualHash) is false)
        {
            return Invalid("Telegram hash is invalid");
        }

        if (IsAuthDateValid(parameters) is false)
        {
            return Invalid("Telegram auth_date is missing, expired or invalid");
        }

        if (parameters.TryGetValue(UserParameterName, out var userJson) is false)
        {
            return Invalid("Telegram user is missing");
        }

        try
        {
            var user = JsonSerializer.Deserialize<TelegramWebAppUser>(userJson);
            return user?.Id > 0 ? user.Id : Invalid("Telegram user ID is invalid");
        }
        catch (JsonException)
        {
            return Invalid("Telegram user JSON is invalid");
        }
    }

    private bool IsAuthDateValid(IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue(AuthDateParameterName, out var authDateText) is false ||
            long.TryParse(authDateText, NumberStyles.None, CultureInfo.InvariantCulture, out var authDateSeconds) is false)
        {
            return false;
        }

        DateTimeOffset authDate;
        try
        {
            authDate = DateTimeOffset.FromUnixTimeSeconds(authDateSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        return authDate <= now.Add(option.ClockSkew) && authDate >= now.Subtract(option.TelegramDataMaxAge).Subtract(option.ClockSkew);
    }

    private static Result<Dictionary<string, string>, Failure<UserSignInFailureCode>> ParseParameters(string telegramData)
    {
        if (string.IsNullOrWhiteSpace(telegramData))
        {
            return Invalid("Telegram data is empty");
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in telegramData.Split('&', StringSplitOptions.None))
        {
            var separatorIndex = item.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                return Invalid("Telegram data contains an invalid parameter");
            }

            string key;
            string value;
            try
            {
                key = Decode(item[..separatorIndex]);
                value = Decode(item[(separatorIndex + 1)..]);
            }
            catch (UriFormatException)
            {
                return Invalid("Telegram data contains invalid escaping");
            }

            if (string.IsNullOrEmpty(key) || parameters.TryAdd(key, value) is false)
            {
                return Invalid("Telegram data contains an empty or duplicate parameter");
            }
        }

        return parameters;
    }

    private static string Decode(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] is not '%')
            {
                continue;
            }

            if (index + 2 >= value.Length || IsHex(value[index + 1]) is false || IsHex(value[index + 2]) is false)
            {
                throw new UriFormatException("Invalid percent-encoding");
            }

            index += 2;
        }

        return Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
    }

    private static bool IsHex(char value)
        =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static byte[] ComputeHash(string botToken, string dataCheckString)
    {
        using var secretKeyAlgorithm = new HMACSHA256(Encoding.UTF8.GetBytes(TelegramWebAppData));
        var secretKey = secretKeyAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(botToken));

        using var dataHashAlgorithm = new HMACSHA256(secretKey);
        return dataHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
    }

    private static bool TryDecodeHash(string hashText, out byte[] hash)
    {
        if (hashText.Length is not 64)
        {
            hash = [];
            return false;
        }

        try
        {
            hash = Convert.FromHexString(hashText);
            return true;
        }
        catch (FormatException)
        {
            hash = [];
            return false;
        }
    }

    private static Failure<UserSignInFailureCode> Invalid(string message)
        =>
        Failure.Create(UserSignInFailureCode.InvalidTelegramData, message);
}

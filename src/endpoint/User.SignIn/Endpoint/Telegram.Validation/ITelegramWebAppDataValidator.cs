using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITelegramWebAppDataValidator
{
    Result<long, Failure<UserSignInFailureCode>> Validate(string telegramData);
}

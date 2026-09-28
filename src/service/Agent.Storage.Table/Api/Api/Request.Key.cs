using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GarageGroup.Internal.Timesheet;

partial class AgentRequestTableApi
{
    private static string GetRequestPartitionKey(AgentUserContext context)
        =>
        context.BotId.ToString(CultureInfo.InvariantCulture);

    private static string GetRequestId(long botId, long telegramUpdateId)
    {
        var source = string.Create(
            CultureInfo.InvariantCulture,
            $"{botId}:{telegramUpdateId}");

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}

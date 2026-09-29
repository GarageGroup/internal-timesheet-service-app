using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionTableApi
{
    private static string GetPartitionKey(AgentUserContext context)
        =>
        context.BotId.ToString(CultureInfo.InvariantCulture);

    private static string GetRowKey(Guid actionId)
        =>
        actionId.ToString("N", CultureInfo.InvariantCulture);
}

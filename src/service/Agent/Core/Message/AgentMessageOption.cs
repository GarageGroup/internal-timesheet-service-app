using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageOption(TimeZoneInfo TimeZone, int MaxTextLength)
{
    public int MaxHistoryMessageCount { get; init; }

    public bool WritePreparationEnabled { get; init; }
}

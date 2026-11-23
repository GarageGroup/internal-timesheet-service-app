using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageIn(string Text, string? Locale)
{
    public FlatArray<AgentChatMessage> History { get; init; }
}

using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentConversationGetOut(FlatArray<AgentChatMessage> Messages, string? Version);

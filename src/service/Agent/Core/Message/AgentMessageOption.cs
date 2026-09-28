using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageOption(TimeZoneInfo TimeZone, int MaxTextLength);

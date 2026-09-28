namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentRequestCreateIn(long TelegramUpdateId, string Text, string? Locale);

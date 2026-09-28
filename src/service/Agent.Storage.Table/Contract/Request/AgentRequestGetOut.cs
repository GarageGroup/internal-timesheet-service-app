namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentRequestGetOut(
    string RequestId,
    long TelegramUpdateId,
    string Text,
    string? Locale,
    AgentRequestStatus Status,
    string? ResponseText,
    string? FailureCode,
    string Version);

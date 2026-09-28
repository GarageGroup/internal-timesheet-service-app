namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentRequestUpdateIn(
    string RequestId,
    string ExpectedVersion,
    AgentRequestStatus ExpectedStatus,
    AgentRequestStatus Status,
    string? ResponseText,
    string? FailureCode);

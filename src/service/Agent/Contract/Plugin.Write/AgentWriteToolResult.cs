namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentWriteToolResult<TData>
    where TData : class
{
    public required bool IsSuccess { get; init; }

    public TData? Data { get; init; }

    public string? ErrorCode { get; init; }
}

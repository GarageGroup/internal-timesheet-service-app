namespace GarageGroup.Internal.Timesheet;

public readonly record struct AgentReadToolResult<TData>
    where TData : struct
{
    public required bool IsSuccess { get; init; }

    public TData? Data { get; init; }

    public string? ErrorCode { get; init; }
}

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    private static AgentReadToolResult<TData> BuildSuccess<TData>(TData data)
        where TData : struct
        =>
        new()
        {
            IsSuccess = true,
            Data = data
        };

    private static AgentReadToolResult<TData> BuildFailure<TData, TFailureCode>(TFailureCode failureCode)
        where TData : struct
        where TFailureCode : struct
        =>
        new()
        {
            IsSuccess = false,
            ErrorCode = failureCode.ToString()
        };
}

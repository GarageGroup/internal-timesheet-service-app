namespace GarageGroup.Internal.Timesheet;

partial class AgentWritePlugin
{
    private static AgentWriteToolResult<TData> BuildSuccess<TData>(TData data)
        where TData : class
        =>
        new()
        {
            IsSuccess = true,
            Data = data
        };

    private static AgentWriteToolResult<TData> BuildFailure<TData, TFailureCode>(TFailureCode failureCode)
        where TData : class
        where TFailureCode : struct
        =>
        new()
        {
            IsSuccess = false,
            ErrorCode = failureCode.ToString()
        };
}

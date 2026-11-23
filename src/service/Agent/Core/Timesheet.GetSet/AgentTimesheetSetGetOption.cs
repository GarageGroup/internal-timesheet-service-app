namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTimesheetSetGetOption
{
    public int MaxDateRangeInDays { get; init; } = 31;
}

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentTagSetGetOption
{
    public int MaxTags { get; init; } = 20;
}

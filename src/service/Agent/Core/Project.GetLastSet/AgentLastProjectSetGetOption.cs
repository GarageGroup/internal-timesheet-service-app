namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentLastProjectSetGetOption
{
    public int DefaultTop { get; init; } = 10;

    public int MaxTop { get; init; } = 20;
}

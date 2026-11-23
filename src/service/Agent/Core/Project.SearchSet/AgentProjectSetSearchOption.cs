namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentProjectSetSearchOption
{
    public int DefaultTop { get; init; } = 10;

    public int MaxTop { get; init; } = 20;

    public int MaxSearchTextLength { get; init; } = 100;
}

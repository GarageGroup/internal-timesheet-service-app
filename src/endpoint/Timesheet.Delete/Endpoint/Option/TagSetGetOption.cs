namespace GarageGroup.Internal.Timesheet;

public sealed record class TagSetGetOption
{
    public TagSetGetOption(int daysPeriod)
        =>
        DaysPeriod = daysPeriod;

    public int DaysPeriod { get; }
}
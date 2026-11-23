using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentPeriodItem
{
    public AgentPeriodItem([AllowNull] string name, DateOnly dateFrom, DateOnly dateTo)
    {
        Name = name.OrEmpty();
        From = dateFrom;
        To = dateTo;
    }

    public string Name { get; }

    public DateOnly From { get; }

    public DateOnly To { get; }
}

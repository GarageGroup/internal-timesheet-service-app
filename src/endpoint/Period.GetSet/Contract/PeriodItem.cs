using GarageGroup.Infra;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

using static PeriodSetGetMetadata;

public sealed record class PeriodItem
{
    public PeriodItem([AllowNull] string name, DateOnly dateFrom, DateOnly dateTo)
    {
        Name = name.OrEmpty();
        From = dateFrom;
        To = dateTo;
    }

    [SwaggerDescription(Out.NameDescription)]
    [StringExample(Out.NameExample)]
    public string Name { get; }

    [SwaggerDescription(Out.DateFromDescription)]
    [StringExample(Out.DateFromExample)]
    public DateOnly From { get; }

    [SwaggerDescription(Out.DateToDescription)]
    [StringExample(Out.DateToExample)]
    public DateOnly To { get; }
}
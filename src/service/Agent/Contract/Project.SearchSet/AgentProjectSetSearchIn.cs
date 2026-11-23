using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentProjectSetSearchIn
{
    public AgentProjectSetSearchIn([AllowNull] string searchText, int? top)
    {
        SearchText = searchText.OrEmpty();
        Top = top;
    }

    public string SearchText { get; }

    public int? Top { get; }
}

using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentProjectItem
{
    public AgentProjectItem(Guid id, [AllowNull] string name, ProjectType type)
    {
        Id = id;
        Name = name.OrEmpty();
        Type = type;
    }

    public Guid Id { get; }

    public string Name { get; }

    public ProjectType Type { get; }

    public string? Comment { get; init; }
}

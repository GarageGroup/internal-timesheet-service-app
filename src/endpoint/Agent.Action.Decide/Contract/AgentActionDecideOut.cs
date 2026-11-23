using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentActionDecideOut
{
    public AgentActionDecideOut(Guid actionId, AgentActionDecision decision)
    {
        ActionId = actionId;
        Decision = decision;
    }

    [JsonBodyOut]
    public Guid ActionId { get; }

    [JsonBodyOut]
    public AgentActionDecision Decision { get; }

    [JsonBodyOut]
    public DateOnly Date { get; init; }

    [JsonBodyOut]
    public bool TimesheetsLoaded { get; init; }

    [JsonBodyOut]
    public AgentActionTimesheetOut[] Timesheets { get; init; } = [];
}

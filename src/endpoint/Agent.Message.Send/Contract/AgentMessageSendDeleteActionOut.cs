using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageSendDeleteActionOut
{
    [JsonBodyOut]
    public required Guid ActionId { get; init; }

    [JsonBodyOut]
    public required Guid TimesheetId { get; init; }

    [JsonBodyOut]
    public required DateOnly Date { get; init; }

    [JsonBodyOut]
    public required string ProjectName { get; init; }

    [JsonBodyOut]
    public required decimal Duration { get; init; }

    [JsonBodyOut]
    public required string Description { get; init; }

    [JsonBodyOut]
    public required DateTimeOffset ExpiresAt { get; init; }
}

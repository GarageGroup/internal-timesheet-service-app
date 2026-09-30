using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageSendOut
{
    public AgentMessageSendOut(
        [AllowNull] string text,
        AgentMessageSendCreateActionOut? preparedCreateAction = null,
        AgentMessageSendDeleteActionOut? preparedDeleteAction = null,
        AgentMessageSendUpdateActionOut? preparedUpdateAction = null)
    {
        Text = text.OrEmpty();
        PreparedCreateAction = preparedCreateAction;
        PreparedDeleteAction = preparedDeleteAction;
        PreparedUpdateAction = preparedUpdateAction;
    }

    [JsonBodyOut]
    public string Text { get; }

    [JsonBodyOut]
    public AgentMessageSendCreateActionOut? PreparedCreateAction { get; }

    [JsonBodyOut]
    public AgentMessageSendDeleteActionOut? PreparedDeleteAction { get; }

    [JsonBodyOut]
    public AgentMessageSendUpdateActionOut? PreparedUpdateAction { get; }
}

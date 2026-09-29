using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageSendOut
{
    public AgentMessageSendOut([AllowNull] string text, AgentMessageSendActionOut? preparedAction = null)
    {
        Text = text.OrEmpty();
        PreparedAction = preparedAction;
    }

    [JsonBodyOut]
    public string Text { get; }

    [JsonBodyOut]
    public AgentMessageSendActionOut? PreparedAction { get; }
}

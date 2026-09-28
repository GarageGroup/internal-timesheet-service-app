using System;
using System.Diagnostics.CodeAnalysis;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageSendOut
{
    public AgentMessageSendOut([AllowNull] string text)
        =>
        Text = text.OrEmpty();

    [JsonBodyOut]
    public string Text { get; }
}

using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentAudioTranscribeOption(int MaxFileSizeBytes, FlatArray<string> SupportedMimeTypes)
{
    public static AgentAudioTranscribeOption Default { get; } = new(
        5 * 1024 * 1024,
        ["audio/ogg", "audio/mpeg", "audio/mp4", "audio/wav", "audio/webm"]);

    public bool Enabled { get; init; } = true;
}

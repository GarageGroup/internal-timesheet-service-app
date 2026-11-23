using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentAudioTranscribeIn(
    ReadOnlyMemory<byte> Audio,
    string MimeType,
    string FileName,
    string Language);

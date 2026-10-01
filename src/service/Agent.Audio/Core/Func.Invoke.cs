using GarageGroup.Infra;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class AgentAudioTranscribeFunc
{
    public ValueTask<Result<AgentAudioTranscribeOut, Failure<AgentAudioTranscribeFailureCode>>> InvokeAsync(
        AgentAudioTranscribeIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            Validate)
        .ForwardValue(
            TranscribeAsync);

    private Result<AgentAudioTranscribeIn, Failure<AgentAudioTranscribeFailureCode>> Validate(
        AgentAudioTranscribeIn input)
    {
        if (option.Enabled is false)
        {
            return Failure.Create(AgentAudioTranscribeFailureCode.InvalidAudio, "Audio transcription is disabled");
        }

        if (input.Audio.IsEmpty ||
            string.IsNullOrWhiteSpace(input.MimeType) ||
            string.IsNullOrWhiteSpace(input.FileName))
        {
            return Failure.Create(AgentAudioTranscribeFailureCode.InvalidAudio, "Audio data and file name must be specified");
        }

        if (input.Audio.Length > option.MaxFileSizeBytes)
        {
            return Failure.Create(AgentAudioTranscribeFailureCode.AudioTooLarge, "Audio file is too large");
        }

        if (option.SupportedMimeTypes.AsEnumerable().Contains(input.MimeType, StringComparer.OrdinalIgnoreCase) is false)
        {
            return Failure.Create(AgentAudioTranscribeFailureCode.UnsupportedFormat, "Audio format is not supported");
        }

        return input;
    }

    private async ValueTask<Result<AgentAudioTranscribeOut, Failure<AgentAudioTranscribeFailureCode>>> TranscribeAsync(
        AgentAudioTranscribeIn input,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = new OpenAIAudioToTextExecutionSettings(input.FileName);
            if (string.IsNullOrWhiteSpace(input.Language) is false)
            {
                settings.Language = input.Language;
            }
            var result = await audioToTextService.GetTextContentAsync(
                new AudioContent(input.Audio, input.MimeType),
                settings,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                return Failure.Create(AgentAudioTranscribeFailureCode.EmptyTranscript, "Audio transcript is empty");
            }

            return new AgentAudioTranscribeOut(result.Text.Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure.Create(AgentAudioTranscribeFailureCode.Unknown, "Audio transcription failed", exception);
        }
    }
}

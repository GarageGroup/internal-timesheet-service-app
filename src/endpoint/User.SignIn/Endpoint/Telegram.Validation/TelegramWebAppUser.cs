using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TelegramWebAppUser
{
    [JsonPropertyName("id")]
    public long Id { get; init; }
}

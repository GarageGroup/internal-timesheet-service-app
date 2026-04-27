using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GarageGroup.Infra;
using Microsoft.OpenApi;

namespace GarageGroup.Internal.Timesheet;

[JsonConverter(typeof(InnerJsonConverter))]
public sealed record class NotificationTime : IOpenApiSchemaProvider
{
    private static readonly Dictionary<string, NotificationTime> NotificationTimes;

    public static NotificationTime Msk18 { get; }

    public static NotificationTime Msk19 { get; }

    public static NotificationTime Msk20 { get; }

    public static NotificationTime Msk21 { get; }

    public static OpenApiSchema GetSchema(bool nullable, JsonNode? example = null, string? description = null)
    {
        return new()
        {
            Type = JsonSchemaType.String,
            Enum = NotificationTimes.Select(ToOpenApiString).ToArray(),
            Example = example ?? JsonValue.Create(Msk18.DisplayText),
            Description = description
        };

        static JsonValue ToOpenApiString(KeyValuePair<string, NotificationTime> pair)
            =>
            JsonValue.Create(pair.Key);
    }

    static NotificationTime()
    {
        Msk18 = new(18);
        Msk19 = new(19);
        Msk20 = new(20);
        Msk21 = new(21);

        NotificationTimes = new()
        {
            [Msk18.DisplayText] = Msk18,
            [Msk19.DisplayText] = Msk19,
            [Msk20.DisplayText] = Msk20,
            [Msk21.DisplayText] = Msk21
        };
    }

    private NotificationTime(int hour)
        =>
        Time = new(hour, 00);

    public string DisplayText
        =>
        Time.ToString("HH:mm");

    public TimeOnly Time { get; }

    private sealed class InnerJsonConverter : JsonConverter<NotificationTime>
    {
        public override NotificationTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType is not JsonTokenType.String)
            {
                throw new JsonException($"JSON token type must be string for {nameof(NotificationTime)}");
            }

            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (NotificationTimes.TryGetValue(text, out var notificationTime) is false)
            {
                throw new JsonException($"An unexpected notification time value: {text}");
            }

            return notificationTime;
        }

        public override void Write(Utf8JsonWriter writer, NotificationTime value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.DisplayText);
            }
        }
    }
}
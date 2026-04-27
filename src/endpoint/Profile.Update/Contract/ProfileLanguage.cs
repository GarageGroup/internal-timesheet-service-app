using GarageGroup.Infra;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

[JsonConverter(typeof(InnerJsonConverter))]
public sealed record class ProfileLanguage : IOpenApiSchemaProvider
{
    private static readonly Dictionary<string, ProfileLanguage> ProfileLanguages;

    public static ProfileLanguage English { get; }

    public static ProfileLanguage Russian { get; }

    public static OpenApiSchema GetSchema(bool nullable, JsonNode? example = null, string? description = null)
    {
        return new()
        {
            Type = JsonSchemaType.String,
            Enum = ProfileLanguages.Select(ToOpenApiString).ToArray(),
            Example = example ?? JsonValue.Create(English.Code),
            Description = description
        };

        static JsonValue ToOpenApiString(KeyValuePair<string, ProfileLanguage> pair)
            =>
            JsonValue.Create(pair.Key);
    }

    static ProfileLanguage()
    {
        English = new("en");
        Russian = new("ru");

        ProfileLanguages = new(StringComparer.InvariantCultureIgnoreCase)
        {
            [English.Code] = English,
            [Russian.Code] = Russian,
        };
    }

    private ProfileLanguage(string code)
        =>
        Code = code;

    public string Code { get; }

    private sealed class InnerJsonConverter : JsonConverter<ProfileLanguage>
    {
        public override ProfileLanguage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType is not JsonTokenType.String)
            {
                throw new JsonException($"JSON token type must be string for {nameof(ProfileLanguage)}");
            }

            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (ProfileLanguages.TryGetValue(text, out var profileLanguage) is false)
            {
                throw new JsonException($"An unexpected language code value: {text}");
            }

            return profileLanguage;
        }

        public override void Write(Utf8JsonWriter writer, ProfileLanguage value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Code);
            }
        }
    }
}
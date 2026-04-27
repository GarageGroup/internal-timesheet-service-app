using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace GarageGroup.Internal.Timesheet;

internal static partial class IsSuccessMiddleware
{
    private const string IsSuccessField = "isSuccess";

    private const string IsSuccessFalseJson = "{\"isSuccess\":false}";

    private const string IsSuccessTrueJson = "{\"isSuccess\":true}";

    private const string ContentType = "application/json";

    private const int SuccessStatusCode = 200;

    private const int NoContentStatusCode = 204;

    private const string ProblemDetailsSchemaName = "ProblemDetails";

    private const string Json = "json";

    private static OpenApiSchema CreateIsSuccessSchema(bool value)
    =>
    new()
    {
        Type = JsonSchemaType.Boolean,
        Example = JsonValue.Create(value),
        Description = "Indicates whether the operation was successful."
    };

    private static readonly JsonSerializerOptions SerializerOptions
        =
        new()
        {
            WriteIndented = true
        };

    private static void InnerConfigureSwagger(OpenApiDocument openApiDocument)
    {
        var paths = openApiDocument.Paths.Values;

        foreach (var path in paths)
        {
            if (path.Operations?.Count is not > 0)
            {
                continue;
            }

            foreach (var operation in path.Operations.Values)
            {
                var changesToMake = new HashSet<KeyValuePair<string, IOpenApiResponse>>();
                var keysToRemove = new HashSet<string>();

                foreach (var response in operation.Responses ?? [])
                {
                    var responseKey = response.GetResponseKey();
                    if (response.Value.Content?.Count > 0)
                    {
                        foreach (var content in response.Value.Content)
                        {
                            if (content.Key.Contains(Json, StringComparison.InvariantCultureIgnoreCase) is false)
                            {
                                continue;
                            }

                            var successSchema = CreateIsSuccessSchema(true);
                            content.Value?.Schema?.Properties?.InsertPropertySchema(IsSuccessField, successSchema);
                        }

                        continue;
                    }

                    var responseValue = new OpenApiResponse
                    {
                        Description = response.Value.Description,
                        Headers = response.Value.Headers,
                        Content = new Dictionary<string, IOpenApiMediaType>
                        {
                            [ContentType] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Properties = new Dictionary<string, IOpenApiSchema>
                                    {
                                        [IsSuccessField] = CreateIsSuccessSchema(responseKey?.IsSuccessStatusKey() is true)
                                    }
                                }
                            }
                        }
                    };

                    keysToRemove.Add(response.Key);

                    if (responseKey is NoContentStatusCode)
                    {
                        responseValue.Description = "Success";
                        changesToMake.Add(new(SuccessStatusCode.ToString(), responseValue));
                    }
                    else
                    {
                        changesToMake.Add(new(response.Key, responseValue));
                    }
                }

                if (operation.Responses is null)
                {
                    continue;
                }

                foreach (var key in keysToRemove)
                {
                    operation.Responses.Remove(key);
                }

                foreach (var change in changesToMake)
                {
                    operation.Responses[change.Key] = change.Value;
                }

                operation.Responses.OrderByKeys();
            }
        }

        if (openApiDocument.Components?.Schemas?.TryGetValue(ProblemDetailsSchemaName, out var problemDetails) is true)
        {
            problemDetails.Properties?.InsertPropertySchema(IsSuccessField, CreateIsSuccessSchema(false));
        }
    }

    private static void InsertPropertySchema(
        this IDictionary<string, IOpenApiSchema> properties, string name, OpenApiSchema schema) 
    {
        var existing = properties.ToArray();

        properties.Clear();
        properties[name] = schema;

        foreach (var property in existing)
        {
            if (property.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            properties[property.Key] = property.Value;
        }
    }

    private static void OrderByKeys(this OpenApiResponses responses)
    {
        var sorted = responses.OrderBy(static kv => kv.Key).ToArray();

        responses.Clear();

        foreach (var kv in sorted)
        {
            responses[kv.Key] = kv.Value;
        }
    }

    private static async Task AddIsSuccessFieldInResponseBodyAsync(HttpContext context, RequestDelegate next)
    {
        var originalBodyStream = context.Response.Body;
        using var newBodyStream = new MemoryStream();
        context.Response.Body = newBodyStream;

        await next.Invoke(context);

        if (context.Response.ContentType?.Contains(Json, StringComparison.InvariantCultureIgnoreCase) is false)
        {
            return;
        }

        var isSuccess = context.Response.StatusCode.IsSuccessStatusKey();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync(context.RequestAborted);
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        if (string.IsNullOrEmpty(responseBody) is false)
        {
            var originalJson = JsonDocument.Parse(responseBody).RootElement;

            var modifiedJson = new Dictionary<string, JsonElement>
            {
                [IsSuccessField] = JsonSerializer.SerializeToElement(isSuccess)
            };

            foreach (var property in originalJson.EnumerateObject())
            {
                modifiedJson[property.Name] = property.Value.Clone();
            }

            var modifiedResponse = JsonSerializer.Serialize(modifiedJson, SerializerOptions);
            await WriteResponseAsync(context.Response, originalBodyStream, modifiedResponse, context.RequestAborted);
        }
        else
        {
            context.Response.StatusCode = context.Response.StatusCode is NoContentStatusCode ? SuccessStatusCode : context.Response.StatusCode;
            var modifiedResponse = isSuccess ? IsSuccessTrueJson : IsSuccessFalseJson;
            await WriteResponseAsync(context.Response, originalBodyStream, modifiedResponse, context.RequestAborted);
        }
    }

    private static Task WriteResponseAsync(HttpResponse response, Stream body, string modifiedResponse, CancellationToken cancellationToken)
    {
        response.ContentType = ContentType;
        response.ContentLength = null;
        response.Body = body;
        return response.WriteAsync(modifiedResponse, cancellationToken);
    }

    private static int? GetResponseKey(this KeyValuePair<string, IOpenApiResponse> response)
        =>
        int.TryParse(response.Key, out var value) ? value : null;

    private static bool IsSuccessStatusKey(this int key)
        =>
        key is >= 200 and < 300;
}
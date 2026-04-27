using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using GarageGroup.Infra.Endpoint;
using Microsoft.OpenApi;

namespace GarageGroup.Internal.Timesheet;

public abstract record class BaseSubscriptionData : IEndpointBodyParser<BaseSubscriptionData>, IEndpointBodyMetadataProvider
{
    private static readonly JsonSerializerOptions SerializerOptions
        =
        EndpointDeserializer.CreateDeafultOptions();

    public static OpenApiRequestBody GetEndpointBodyMetadata()
        =>
        new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                [$"{MediaTypeNames.Application.Json}?type={nameof(NotificationType.DailyNotification)}"] = DailyNotificationMetadata.MediaType,
                [$"{MediaTypeNames.Application.Json}?type={nameof(NotificationType.WeeklyNotification)}"] = WeeklyNotificationMetadata.MediaType
            }
        };

    public static async ValueTask<Result<BaseSubscriptionData, Failure<Unit>>> ParseAsync(
        EndpointRequest request, CancellationToken cancellationToken)
    {
        var documentResult = await request.ParseDocumentAsync(cancellationToken).ConfigureAwait(false);
        return documentResult.Forward(DeserializeSubscriptionData);
    }

    public abstract NotificationType NotificationType { get; }

    public abstract INotificationUserPreference? UserPreference { get; }

    private static Result<BaseSubscriptionData, Failure<Unit>> DeserializeSubscriptionData(
        JsonDocument? jsonDocument)
    {
        if (jsonDocument is null)
        {
            return Failure.Create("Request json document must be specified");
        }

        var typeResult = jsonDocument.GetNullableEnumOrFailure<NotificationType>("notificationType");
        return typeResult.Forward(DeserializeNotificationTypeData);

        Result<BaseSubscriptionData, Failure<Unit>> DeserializeNotificationTypeData(NotificationType? notificationType)
            =>
            notificationType switch
            {
                NotificationType.DailyNotification => DailyNotificationSubscriptionData.DeserializeOrFailure(jsonDocument, SerializerOptions),
                NotificationType.WeeklyNotification => WeeklyNotificationSubscriptionData.DeserializeOrFailure(jsonDocument, SerializerOptions),
                _ => Failure.Create($"An unexpected notification type: {notificationType}")
            };
    }
}
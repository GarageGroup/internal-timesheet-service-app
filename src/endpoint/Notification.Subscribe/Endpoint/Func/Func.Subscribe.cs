using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class NotificationSubscribeFunc
{
    public ValueTask<Result<Unit, Failure<NotificationSubscribeFailureCode>>> InvokeAsync(
        NotificationSubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateAndMapToJsonDto)
        .MapSuccess(
            json => new NotificationData(input, json))
        .Forward(
            InnerInvokeAsync);

    private Task<Result<Unit, Failure<NotificationSubscribeFailureCode>>> InnerInvokeAsync(
        NotificationData input, CancellationToken cancellationToken) 
        => 
        AsyncPipeline.Pipe(
            input.Input, cancellationToken)
        .PipeParallel(
            FindBotUserIdAsync,
            FindNotificationTypeIdAsync)
        .MapSuccess(
            @out => NotificationSubscriptionJson.BuildDataverseUpdateInput(
                botUserId: @out.Item1,
                typeId: @out.Item2,
                subscription: input.Subscription))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.WithFailureCode(NotificationSubscribeFailureCode.Unknown));

    private Task<Result<Guid, Failure<NotificationSubscribeFailureCode>>> FindBotUserIdAsync(
        NotificationSubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            input => TelegramBotUserJson.BuildGetInput(input.SystemUserId, option.BotId))
        .PipeValue(
            dataverseApi.GetEntityAsync<TelegramBotUserJson>)
        .Map(
            static response => response.Value.Id,
            static failure => failure.MapFailureCode(MapFailureCodeWhenFindingSubscribeUser));

    private Task<Result<Guid, Failure<NotificationSubscribeFailureCode>>> FindNotificationTypeIdAsync(
        NotificationSubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input.SubscriptionData.NotificationType, cancellationToken)
        .Pipe(
            MapToNotificationTypeKey)
        .Map(
            NotificationTypeJson.BuildGetInput,
            static failure => failure.WithFailureCode(NotificationSubscribeFailureCode.NotificationTypeInvalid))
        .ForwardValue(
            dataverseApi.GetEntityAsync<NotificationTypeJson>,
            static failure => failure.MapFailureCode(MapFailureCodeWhenFindingSubscribeNotificationType))
        .MapSuccess(
            static response => response.Value.Id);

    private static NotificationSubscribeFailureCode MapFailureCodeWhenFindingSubscribeUser(DataverseFailureCode failureCode) 
        => 
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => NotificationSubscribeFailureCode.BotUserNotFound,
            _ => NotificationSubscribeFailureCode.Unknown 
        };

    private static NotificationSubscribeFailureCode MapFailureCodeWhenFindingSubscribeNotificationType(DataverseFailureCode failureCode)
        => 
        failureCode switch
        { 
            DataverseFailureCode.RecordNotFound => NotificationSubscribeFailureCode.NotificationTypeNotFound, 
            _ => NotificationSubscribeFailureCode.Unknown
        };
}

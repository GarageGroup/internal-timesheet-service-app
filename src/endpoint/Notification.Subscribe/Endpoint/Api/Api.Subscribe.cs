using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class NotificationApi
{
    public ValueTask<Result<Unit, Failure<NotificationSubscribeFailureCode>>> SubscribeAsync(
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
        AsyncPipeline.Pipe<Unit>(
            default, cancellationToken)
        .PipeValue(
            botApi.GetBotInfoAsync)
        .Map(
            bot => TelegramBotUserJson.BuildGetInput(input.SystemUserId, bot.Id),
            static failure => failure.WithFailureCode<NotificationSubscribeFailureCode>(default))
        .ForwardValue(
            dataverseApi.GetEntityAsync<TelegramBotUserJson>,
            static failure => failure.MapFailureCode(MapFailureCodeWhenFindingSubscribeUser))
        .MapSuccess(
            static response => response.Value.Id);

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

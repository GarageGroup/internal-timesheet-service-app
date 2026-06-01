using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class NotificationApi
{
    public ValueTask<Result<Unit, Failure<NotificationUnsubscribeFailureCode>>> UnsubscribeAsync(
        NotificationUnsubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeParallel(
            FindBotUserIdAsync, 
            FindNotificationTypeIdAsync)
        .Forward(
            UpdateSubscriptionAsync);

    private Task<Result<Guid, Failure<NotificationUnsubscribeFailureCode>>> FindBotUserIdAsync(
        NotificationUnsubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe<Unit>(
            default, cancellationToken)
        .PipeValue(
            botApi.GetBotInfoAsync)
        .Map(
            bot => TelegramBotUserJson.BuildGetInput(input.SystemUserId, bot.Id),
            static failure => failure.WithFailureCode<NotificationUnsubscribeFailureCode>(default))
        .ForwardValue(
            dataverseApi.GetEntityAsync<TelegramBotUserJson>,
            static failure => failure.MapFailureCode(MapFailureCodeWhenFindingUnsubscribeUser))
        .MapSuccess(
            static response => response.Value.Id);

    private Task<Result<Guid, Failure<NotificationUnsubscribeFailureCode>>> FindNotificationTypeIdAsync(
        NotificationUnsubscribeIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input.NotificationType, cancellationToken)
        .Pipe(
            MapToNotificationTypeKey)
        .Map(
            NotificationTypeJson.BuildGetInput,
            static failure => failure.WithFailureCode(NotificationUnsubscribeFailureCode.NotificationTypeInvalid))
        .ForwardValue(
            dataverseApi.GetEntityAsync<NotificationTypeJson>,
            static failure => failure.MapFailureCode(MapFailureCodeWhenFindingUnsubscribeNotificationType))
        .MapSuccess(
            static response => response.Value.Id);

    private Task<Result<Unit, Failure<NotificationUnsubscribeFailureCode>>> UpdateSubscriptionAsync(
        (Guid BotUserId, Guid TypeId) input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => NotificationSubscriptionJson.BuildDataverseUpdateInput(
                botUserId: @in.BotUserId,
                typeId: @in.TypeId,
                subscription: new()
                {
                    IsDisabled = true
                },
                operationType: DataverseUpdateOperationType.Update))
        .PipeValue(
            dataverseApi.UpdateEntityAsync)
        .Recover(
            static failure => failure.FailureCode switch
            {
                DataverseFailureCode.RecordNotFound => Result.Success<Unit>(default).With<Failure<NotificationUnsubscribeFailureCode>>(),
                _ => failure.WithFailureCode(NotificationUnsubscribeFailureCode.Unknown)
            });

    private static NotificationUnsubscribeFailureCode MapFailureCodeWhenFindingUnsubscribeUser(DataverseFailureCode failureCode) 
        => 
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => NotificationUnsubscribeFailureCode.BotUserNotFound, 
            _ => NotificationUnsubscribeFailureCode.Unknown 
        };

    private static NotificationUnsubscribeFailureCode MapFailureCodeWhenFindingUnsubscribeNotificationType(DataverseFailureCode failureCode)
        => 
        failureCode switch
        { 
            DataverseFailureCode.RecordNotFound => NotificationUnsubscribeFailureCode.NotificationTypeNotFound, 
            _ => NotificationUnsubscribeFailureCode.Unknown
        };
}

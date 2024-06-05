using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[Endpoint(EndpointMethod.Post, "/notification/subscribe", Description = "Subscribe bot user to notification")]
[EndpointTag("Notification")]
public interface INotificationSubscribeFunc
{
    ValueTask<Result<Unit, Failure<NotificationSubscribeFailureCode>>> InvokeAsync(
        NotificationSubscribeIn input, CancellationToken cancellationToken);
}
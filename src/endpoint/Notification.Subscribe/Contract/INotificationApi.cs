using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[EndpointSet]
public interface INotificationApi : INotificationSubscribeFunc, INotificationUnsubscribeFunc;
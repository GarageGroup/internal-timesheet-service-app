using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static NotificationSubscribeMetadata;

[Endpoint(
    Func.NameUnsubscribe, EndpointMethod.Post, Func.RouteUnsubscribe, Summary = Func.SummaryUnsubscribe, Description = Func.DescriptionUnsubscribe)]
[EndpointTag(Func.Tag)]
public interface INotificationUnsubscribeFunc
{
    ValueTask<Result<Unit, Failure<NotificationUnsubscribeFailureCode>>> UnsubscribeAsync(
        NotificationUnsubscribeIn input, CancellationToken cancellationToken);
}
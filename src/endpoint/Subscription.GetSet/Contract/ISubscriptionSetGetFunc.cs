using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static SubscriptionSetGetMetadata;

[Endpoint("GetSubscriptions", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface ISubscriptionSetGetFunc
{
    ValueTask<Result<SubscriptionSetGetOut, Failure<SubscriptionSetGetFailureCode>>> InvokeAsync(
        SubscriptionSetGetIn input, CancellationToken cancellationToken);
}

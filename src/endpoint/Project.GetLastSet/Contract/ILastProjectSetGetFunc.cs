using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static LastProjectSetGetMetadata;

[Endpoint("GetLastProjects", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface ILastProjectSetGetFunc
{
    ValueTask<Result<LastProjectSetGetOut, Failure<Unit>>> InvokeAsync(
        LastProjectSetGetIn input, CancellationToken cancellationToken);
}
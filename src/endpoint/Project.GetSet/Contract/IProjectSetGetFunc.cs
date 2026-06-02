using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static ProjectSetGetMetadata;

[Endpoint("GetProjects", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface IProjectSetGetFunc
{
    ValueTask<Result<ProjectSetGetOut, Failure<Unit>>> InvokeAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken);
}
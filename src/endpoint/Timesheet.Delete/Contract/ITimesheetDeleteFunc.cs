using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetDeleteMetadata;

[Endpoint(EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface ITimesheetDeleteFunc
{
    ValueTask<Result<Unit, Failure<TimesheetDeleteFailureCode>>> InvokeAsync(
        TimesheetDeleteIn input, CancellationToken cancellationToken);
}
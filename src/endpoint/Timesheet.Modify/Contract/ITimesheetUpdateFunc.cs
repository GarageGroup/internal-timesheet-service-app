using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetModifyMetadata;

[Endpoint("UpdateTimesheet", EndpointMethod.Post, Func.RouteUpdate, Summary = Func.SummaryUpdate, Description = Func.DescriptionUpdate)]
[EndpointTag(Func.Tag)]
public interface ITimesheetUpdateFunc
{
    ValueTask<Result<Unit, Failure<TimesheetUpdateFailureCode>>> UpdateAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken);
}
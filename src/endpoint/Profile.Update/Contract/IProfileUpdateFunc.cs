using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static ProfileUpdateMetadata;

[Endpoint("UpdateProfile", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface IProfileUpdateFunc
{
    ValueTask<Result<Unit, Failure<ProfileUpdateFailureCode>>> InvokeAsync(
        ProfileUpdateIn input, CancellationToken cancellationToken);
}
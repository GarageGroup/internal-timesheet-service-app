using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static UserSignOutMetadata;

[Endpoint("SignOutUser", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface IUserSignOutFunc
{
    ValueTask<Result<Unit, Failure<Unit>>> InvokeAsync(
        UserSignOutIn input, CancellationToken cancellationToken);
}
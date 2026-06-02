using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

using static UserSignInMetadata;

[Endpoint("SignInUser", EndpointMethod.Post, Func.Route, Summary = Func.Summary, Description = Func.Description)]
[EndpointTag(Func.Tag)]
public interface IUserSignInFunc
{
    ValueTask<Result<Unit, Failure<UserSignInFailureCode>>> InvokeAsync(
        UserSignInIn input, CancellationToken cancellationToken);
}
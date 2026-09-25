using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class AgentProfileGetFunc
{
    public async ValueTask<Result<AgentProfileGetOut, Failure<AgentProfileGetFailureCode>>> InvokeAsync(
        AgentProfileGetIn input,
        CancellationToken cancellationToken)
    {
        var userContextResult = await userContextResolver.ResolveAsync(
            new(input.BotId, input.TelegramUserId, input.TelegramChatId),
            cancellationToken).ConfigureAwait(false);

        if (userContextResult.IsSuccess is false)
        {
            var failure = userContextResult.FailureOrThrow();
            return failure.WithFailureCode(MapUserFailureCode(failure.FailureCode));
        }

        var profileResult = await profileGetFunc.InvokeAsync(
            new(userContextResult.SuccessOrThrow().EntraObjectId),
            cancellationToken).ConfigureAwait(false);

        if (profileResult.IsSuccess)
        {
            var profile = profileResult.SuccessOrThrow();
            return new AgentProfileGetOut(profile.UserName, profile.LanguageCode);
        }

        var profileFailure = profileResult.FailureOrThrow();
        return profileFailure.WithFailureCode(
            profileFailure.FailureCode is ProfileGetFailureCode.NotFound
                ? AgentProfileGetFailureCode.ProfileNotFound
                : AgentProfileGetFailureCode.Unknown);
    }

    private static AgentProfileGetFailureCode MapUserFailureCode(AgentUserContextResolveFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentUserContextResolveFailureCode.InvalidIdentity => AgentProfileGetFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UnsupportedChat => AgentProfileGetFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UserNotLinked => AgentProfileGetFailureCode.UserNotLinked,
            AgentUserContextResolveFailureCode.AmbiguousBinding => AgentProfileGetFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.BindingSignedOut => AgentProfileGetFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.UserDisabled => AgentProfileGetFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.MissingEntraObjectId => AgentProfileGetFailureCode.UserUnavailable,
            _ => AgentProfileGetFailureCode.Unknown
        };
}

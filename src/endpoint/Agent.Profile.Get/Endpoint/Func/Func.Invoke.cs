using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class AgentProfileGetFunc
{
    public ValueTask<Result<AgentProfileGetOut, Failure<AgentProfileGetFailureCode>>> InvokeAsync(
        AgentProfileGetIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => new AgentUserIdentity(@in.BotId, @in.TelegramUserId, @in.TelegramChatId))
        .PipeValue(
            userContextResolver.ResolveAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(MapUserFailureCode))
        .ForwardValue(
            GetProfileAsync)
        .MapSuccess(
            static profile => new AgentProfileGetOut(profile.UserName, profile.LanguageCode));

    private ValueTask<Result<ProfileGetOut, Failure<AgentProfileGetFailureCode>>> GetProfileAsync(
        AgentUserContext context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            new ProfileGetIn(context.EntraObjectId), cancellationToken)
        .PipeValue(
            profileGetFunc.InvokeAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(MapProfileFailureCode));

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

    private static AgentProfileGetFailureCode MapProfileFailureCode(ProfileGetFailureCode failureCode)
        =>
        failureCode switch
        {
            ProfileGetFailureCode.NotFound => AgentProfileGetFailureCode.ProfileNotFound,
            _ => AgentProfileGetFailureCode.Unknown
        };
}

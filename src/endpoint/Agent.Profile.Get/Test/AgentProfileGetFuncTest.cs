using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Profile.Get.Test;

public static class AgentProfileGetFuncTest
{
    private static readonly AgentProfileGetIn SomeInput = new(101, 202, 202);

    [Theory]
    [InlineData(AgentUserContextResolveFailureCode.InvalidIdentity, AgentProfileGetFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UnsupportedChat, AgentProfileGetFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UserNotLinked, AgentProfileGetFailureCode.UserNotLinked)]
    [InlineData(AgentUserContextResolveFailureCode.AmbiguousBinding, AgentProfileGetFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.BindingSignedOut, AgentProfileGetFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.UserDisabled, AgentProfileGetFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.MissingEntraObjectId, AgentProfileGetFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.Unknown, AgentProfileGetFailureCode.Unknown)]
    public static async Task InvokeAsync_UserResolveFailure_ExpectMappedFailure(
        AgentUserContextResolveFailureCode sourceCode,
        AgentProfileGetFailureCode expectedCode)
    {
        var sourceException = new Exception("Some error");
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(sourceCode, "Resolve failed", sourceException));

        var profile = new Mock<IProfileGetFunc>(MockBehavior.Strict);
        var func = new AgentProfileGetFunc(resolver.Object, profile.Object);
        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var failure = actual.FailureOrThrow();

        Assert.Equal(expectedCode, failure.FailureCode);
        Assert.Same(sourceException, failure.SourceException);
    }

    [Fact]
    public static async Task InvokeAsync_UserResolved_ExpectTrustedEntraObjectIdPassedToProfile()
    {
        var entraObjectId = Guid.NewGuid();
        var context = new AgentUserContext(101, 202, 202, Guid.NewGuid(), Guid.NewGuid(), entraObjectId);
        var resolver = BuildResolver(context);
        var profile = new Mock<IProfileGetFunc>();
        _ = profile.Setup(p => p.InvokeAsync(new(entraObjectId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileGetOut("User", "en"));

        var func = new AgentProfileGetFunc(resolver.Object, profile.Object);
        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        profile.Verify(p => p.InvokeAsync(new(entraObjectId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ProfileGetFailureCode.NotFound, AgentProfileGetFailureCode.ProfileNotFound)]
    [InlineData(ProfileGetFailureCode.Unknown, AgentProfileGetFailureCode.Unknown)]
    public static async Task InvokeAsync_ProfileFailure_ExpectMappedFailure(
        ProfileGetFailureCode sourceCode,
        AgentProfileGetFailureCode expectedCode)
    {
        var context = new AgentUserContext(101, 202, 202, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var resolver = BuildResolver(context);
        var profile = new Mock<IProfileGetFunc>();
        _ = profile.Setup(static p => p.InvokeAsync(It.IsAny<ProfileGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(sourceCode, "Profile failed"));

        var func = new AgentProfileGetFunc(resolver.Object, profile.Object);
        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_AllCallsSuccessful_ExpectProfile()
    {
        var context = new AgentUserContext(101, 202, 202, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var resolver = BuildResolver(context);
        var profileResult = new ProfileGetOut("Some user", "ru");
        var profile = new Mock<IProfileGetFunc>();
        _ = profile.Setup(static p => p.InvokeAsync(It.IsAny<ProfileGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileResult);

        var func = new AgentProfileGetFunc(resolver.Object, profile.Object);
        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(new AgentProfileGetOut("Some user", "ru"), actual.SuccessOrThrow());
    }

    private static Mock<IAgentUserContextResolver> BuildResolver(AgentUserContext context)
    {
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        return resolver;
    }
}

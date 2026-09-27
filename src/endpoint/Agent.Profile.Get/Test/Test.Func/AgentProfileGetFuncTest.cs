using System;
using System.Threading;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Profile.Get.Test;

public static partial class AgentProfileGetFuncTest
{
    private static readonly AgentProfileGetIn SomeInput = new(101, 202, 202);

    private static Mock<IAgentUserContextResolver> BuildResolver(AgentUserContext context)
    {
        var resolver = new Mock<IAgentUserContextResolver>();

        _ = resolver
            .Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        return resolver;
    }
}

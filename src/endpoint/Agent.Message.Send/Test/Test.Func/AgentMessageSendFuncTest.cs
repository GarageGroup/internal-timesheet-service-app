using System;
using System.Threading;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Message.Send.Test;

public static partial class AgentMessageSendFuncTest
{
    private static readonly AgentMessageSendIn SomeInput = new(101, 303, 202, 202, "Some question", "ru");

    private static readonly AgentUserContext SomeContext
        =
        new(
            101,
            202,
            202,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

    private static Mock<IAgentUserContextResolver> BuildResolver()
    {
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver
            .Setup(static r => r.ResolveAsync(It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SomeContext);

        return resolver;
    }
}

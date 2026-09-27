using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Identity.Test;

public static partial class AgentUserContextResolverTest
{
    private static readonly AgentUserIdentity SomeIdentity = new(101, 202, 202);

    private static DbAgentUserBinding BuildBinding()
        =>
        new()
        {
            BindingId = Guid.NewGuid(),
            CrmSystemUserId = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid()
        };

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi(
        in Result<FlatArray<DbAgentUserBinding>, Failure<Unit>> result)
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbAgentUserBinding>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

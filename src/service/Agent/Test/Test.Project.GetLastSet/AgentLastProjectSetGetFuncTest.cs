using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentLastProjectSetGetFuncTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("ab0fe73a-0910-4e82-bff8-978d1a06b8a5"),
            crmSystemUserId: new("3523d064-e675-42c4-9574-5c6047d6b80d"),
            entraObjectId: new("6add594a-17db-4d85-aee6-aa9b286b758c"));

    private static readonly AgentLastProjectSetGetOption SomeOption = new()
    {
        DefaultTop = 10,
        MaxTop = 20
    };

    private static Mock<ILastProjectSetGetFunc> BuildMockLastProjectFunc(
        in Result<LastProjectSetGetOut, Failure<Unit>> result)
    {
        var mock = new Mock<ILastProjectSetGetFunc>();

        _ = mock
            .Setup(static f => f.InvokeAsync(It.IsAny<LastProjectSetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTagSetGetFuncTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("b9a3feb2-d2e6-41c8-9316-1fc1575dedf3"),
            crmSystemUserId: new("d9764c5d-ed9e-4059-8345-25bef0cfc255"),
            entraObjectId: new("7d2c1afc-b507-4e4e-abde-8278c37ecb30"));

    private static Mock<ITagSetGetFunc> BuildMockTagFunc(
        in Result<TagSetGetOut, Failure<Unit>> result)
    {
        var mock = new Mock<ITagSetGetFunc>();

        _ = mock
            .Setup(static f => f.InvokeAsync(It.IsAny<TagSetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

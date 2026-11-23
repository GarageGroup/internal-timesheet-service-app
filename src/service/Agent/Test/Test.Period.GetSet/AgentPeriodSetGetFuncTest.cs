using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentPeriodSetGetFuncTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("e31f79ba-eb78-4aaa-a36f-f6fbd65c5814"),
            crmSystemUserId: new("a90e2e31-999a-432a-81fd-c342f6669f09"),
            entraObjectId: new("921c29ea-a14c-494a-971a-747a38f9c4e2"));

    private static Mock<IPeriodSetGetFunc> BuildMockPeriodFunc(
        in Result<PeriodSetGetOut, Failure<Unit>> result)
    {
        var mock = new Mock<IPeriodSetGetFunc>();

        _ = mock
            .Setup(static f => f.InvokeAsync(It.IsAny<Unit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

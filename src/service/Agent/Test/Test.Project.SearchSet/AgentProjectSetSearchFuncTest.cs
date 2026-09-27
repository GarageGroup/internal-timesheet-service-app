using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentProjectSetSearchFuncTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("104b8bfa-74ad-4a07-ad88-e9e04af17d8b"),
            crmSystemUserId: new("b5a73bbb-19de-41a6-961d-cb8d748e8b06"),
            entraObjectId: new("bfcdabc8-08da-43fe-9276-efdc20b25c5d"));

    private static readonly AgentProjectSetSearchOption SomeOption = new()
    {
        DefaultTop = 10,
        MaxTop = 20,
        MaxSearchTextLength = 100
    };

    private static Mock<IProjectSetSearchFunc> BuildMockProjectSearchFunc(
        in Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>> result)
    {
        var mock = new Mock<IProjectSetSearchFunc>();

        _ = mock
            .Setup(static f => f.InvokeAsync(It.IsAny<ProjectSetSearchIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

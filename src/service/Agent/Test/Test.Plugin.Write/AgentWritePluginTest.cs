using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentWritePluginTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 303,
            bindingId: new("80ae312e-305b-49dd-a905-d39e30d11385"),
            crmSystemUserId: new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
            entraObjectId: new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static readonly AgentTimesheetCreatePrepareOut SomeOutput
        =
        new(
            ActionId: new("84e6c5b8-1597-4a2e-821a-f392c8c0ae3d"),
            Date: new(2026, 09, 29),
            ProjectId: new("d9cb8306-dd0c-499b-ad90-44b9a324e30c"),
            ProjectName: "Some project",
            ProjectType: ProjectType.Project,
            Duration: 1.5m,
            Description: "Some description",
            ExpiresAt: new(2026, 09, 29, 12, 10, 00, TimeSpan.Zero));

    private static AgentWritePlugin CreatePlugin(
        Mock<IAgentTimesheetCreatePrepareFunc> prepareFunc,
        AgentPreparedActionCapture? capture = null)
        =>
        new(SomeContext, prepareFunc.Object, capture ?? new());

    private static Mock<IAgentTimesheetCreatePrepareFunc> BuildPrepareFunc(
        in Result<AgentTimesheetCreatePrepareOut, Failure<AgentTimesheetCreatePrepareFailureCode>> result)
    {
        var mock = new Mock<IAgentTimesheetCreatePrepareFunc>();
        _ = mock
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<AgentTimesheetCreatePrepareIn>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}

using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTimesheetCreatePrepareFuncTest
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

    private static readonly AgentTimesheetCreatePrepareIn SomeInput
        =
        new(
            date: new(2026, 09, 29),
            projectId: new("d9cb8306-dd0c-499b-ad90-44b9a324e30c"),
            duration: 1.5m,
            description: "Some description");

    private static readonly DateTimeOffset SomeUtcNow = new(2026, 09, 29, 12, 00, 00, TimeSpan.Zero);

    private static readonly AgentTimesheetCreatePrepareOption SomeOption = new()
    {
        ApprovalTtl = TimeSpan.FromMinutes(10)
    };

    private static AgentTimesheetCreatePrepareFunc BuildFunc(
        in Result<ProjectSetGetOut, Failure<Unit>> projectResult,
        in Result<Unit, Failure<AgentActionStoreFailureCode>> storeResult,
        out Mock<IProjectSetGetFunc> projectFunc,
        out Mock<IAgentActionStore> actionStore)
    {
        projectFunc = new();
        actionStore = new();

        _ = projectFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<ProjectSetGetIn>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectResult);

        _ = actionStore
            .Setup(static s => s.CreateAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<AgentTimesheetCreateAction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storeResult);

        return new(projectFunc.Object, actionStore.Object, new TestDateProvider(SomeUtcNow), SomeOption);
    }

    private sealed class TestDateProvider(DateTimeOffset utcNow) : IDateProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public DateOnly Today
            =>
            DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}

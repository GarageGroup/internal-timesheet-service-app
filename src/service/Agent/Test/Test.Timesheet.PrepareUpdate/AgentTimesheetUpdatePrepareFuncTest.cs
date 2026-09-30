extern alias ProjectGetContract;

using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;
using ProjectGetItem = ProjectGetContract::GarageGroup.Internal.Timesheet.ProjectItem;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTimesheetUpdatePrepareFuncTest
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

    private static readonly AgentTimesheetSetGetItem SomeTimesheet
        =
        new(
            id: new("ff9ad9f7-4592-40f3-b75f-df448355c486"),
            projectId: new("ac65d591-9c16-4c2f-b2ae-54b151b3f9ae"),
            projectType: ProjectType.Project,
            projectName: "Original project",
            duration: 1.5m,
            description: "Original description",
            isActive: true,
            date: new(2026, 09, 30));

    private static readonly DateTimeOffset SomeUtcNow = new(2026, 09, 30, 12, 00, 00, TimeSpan.Zero);

    private static AgentTimesheetUpdatePrepareFunc BuildFunc(
        AgentTimesheetSetGetItem? timesheet,
        ProjectGetItem? project,
        in Result<Unit, Failure<AgentActionStoreFailureCode>> storeResult,
        out Mock<IAgentTimesheetSetGetFunc> timesheetFunc,
        out Mock<IProjectSetGetFunc> projectFunc,
        out Mock<IAgentTimesheetUpdateActionStore> actionStore)
    {
        timesheetFunc = new();
        projectFunc = new();
        actionStore = new();

        _ = timesheetFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<AgentTimesheetSetGetIn>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentTimesheetSetGetOut
            {
                Timesheets = timesheet is null ? default : new[] { timesheet }
            });

        _ = projectFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<ProjectSetGetIn>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectSetGetOut
            {
                Projects = project is null ? default : new[] { project }
            });

        _ = actionStore
            .Setup(static s => s.CreateAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<AgentTimesheetUpdateAction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storeResult);

        return new(
            timesheetFunc.Object,
            projectFunc.Object,
            actionStore.Object,
            new TestDateProvider(SomeUtcNow),
            new() { ApprovalTtl = TimeSpan.FromMinutes(10) });
    }

    private sealed class TestDateProvider(DateTimeOffset utcNow) : IDateProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public DateOnly Today
            =>
            DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}

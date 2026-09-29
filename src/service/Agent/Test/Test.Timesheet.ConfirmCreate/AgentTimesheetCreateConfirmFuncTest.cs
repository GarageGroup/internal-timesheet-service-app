using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTimesheetCreateConfirmFuncTest
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

    private static readonly Guid SomeActionId = new("78302d93-e6dc-4fd6-be63-2480c8984382");

    private static readonly DateTimeOffset SomeUtcNow = new(2026, 09, 29, 12, 00, 00, TimeSpan.Zero);

    private static AgentTimesheetCreateAction BuildAction(
        AgentActionState state = AgentActionState.Pending,
        DateTimeOffset? expiresAt = null,
        string? version = "version-1")
        =>
        new(
            SomeActionId,
            new(2026, 09, 29),
            new("d9cb8306-dd0c-499b-ad90-44b9a324e30c"),
            "Some project",
            ProjectType.Project,
            1.5m,
            "Some description",
            SomeUtcNow.AddMinutes(-1),
            expiresAt ?? SomeUtcNow.AddMinutes(9),
            state,
            version);

    private static AgentTimesheetCreateConfirmFunc BuildFunc(
        in Result<AgentTimesheetCreateAction?, Failure<AgentActionStoreFailureCode>> getResult,
        in Result<Unit, Failure<AgentActionStoreFailureCode>> updateResult,
        out Mock<IAgentActionStore> actionStore)
    {
        actionStore = new();

        _ = actionStore
            .Setup(static s => s.GetAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(getResult);

        _ = actionStore
            .Setup(static s => s.UpdateStateAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        return new(actionStore.Object, new TestDateProvider(SomeUtcNow));
    }

    private sealed class TestDateProvider(DateTimeOffset utcNow) : IDateProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public DateOnly Today
            =>
            DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}

using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTimesheetDeletePrepareFuncTest
{
    private static readonly AgentUserContext SomeContext = new(
        101,
        202,
        303,
        new("80ae312e-305b-49dd-a905-d39e30d11385"),
        new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
        new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static readonly Guid SomeTimesheetId = new("20d96e9d-d8df-4248-82b0-0b1a17f8ea0a");

    private static readonly DateOnly SomeDate = new(2026, 09, 29);

    private static readonly DateTimeOffset SomeUtcNow = new(2026, 09, 29, 12, 00, 00, TimeSpan.Zero);

    private static AgentTimesheetDeletePrepareFunc BuildFunc(
        in Result<AgentTimesheetSetGetOut, Failure<AgentTimesheetSetGetFailureCode>> timesheetResult,
        out Mock<IAgentTimesheetSetGetFunc> timesheetFunc,
        out Mock<IAgentTimesheetDeleteActionStore> actionStore)
    {
        timesheetFunc = new();
        actionStore = new();
        _ = timesheetFunc.Setup(static f => f.InvokeAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentTimesheetSetGetIn>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(timesheetResult);
        _ = actionStore.Setup(static s => s.CreateAsync(
            It.IsAny<AgentUserContext>(),
            It.IsAny<AgentTimesheetDeleteAction>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<Unit>(default));

        return new(timesheetFunc.Object, actionStore.Object, new TestDateProvider(SomeUtcNow), new()
        {
            ApprovalTtl = TimeSpan.FromMinutes(10)
        });
    }

    private sealed class TestDateProvider(DateTimeOffset utcNow) : IDateProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}

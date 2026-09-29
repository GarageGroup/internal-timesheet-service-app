using System;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentMessageFuncTest
{
    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("80ae312e-305b-49dd-a905-d39e30d11385"),
            crmSystemUserId: new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
            entraObjectId: new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static readonly AgentMessageOption SomeOption = new(
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"),
        2000)
    {
        MaxHistoryMessageCount = 20
    };

    private static AgentMessageFunc CreateFunc(
        IChatCompletionService chatService,
        Mock<IAgentKernelFactory>? kernelFactory = null,
        DateOnly? today = null,
        AgentTimesheetCreatePrepareOut? preparedAction = null,
        AgentTimesheetDeletePrepareOut? preparedDeleteAction = null)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);

        var kernel = builder.Build();
        var capture = new AgentPreparedActionCapture();
        if (preparedAction is not null)
        {
            capture.Complete(preparedAction);
        }

        if (preparedDeleteAction is not null)
        {
            capture.Complete(preparedDeleteAction);
        }

        kernelFactory ??= new();
        _ = kernelFactory.Setup(f => f.Create(SomeContext)).Returns(new AgentKernelScope(kernel, capture));

        return new(kernelFactory.Object, new TestDateProvider(today ?? new DateOnly(2026, 9, 28)), SomeOption);
    }

    private sealed class TestDateProvider(DateOnly today) : IDateProvider
    {
        public DateTimeOffset UtcNow
            =>
            new(Today, TimeOnly.MinValue, TimeSpan.Zero);

        public DateOnly Today { get; } = today;
    }
}

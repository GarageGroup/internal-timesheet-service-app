using System;
using Azure.Core;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentKernelFactoryTest
{
    private static readonly Mock<TokenCredential> TokenCredential = new();

    private static readonly AgentFoundryOption SomeOption = new(
        new("https://example.services.ai.azure.com/api/projects/timesheet-test"),
        "some-model",
        "https://ai.azure.com/.default");

    private static readonly AgentUserContext SomeContext
        =
        new(
            botId: 101,
            telegramUserId: 202,
            telegramChatId: 202,
            bindingId: new("80ae312e-305b-49dd-a905-d39e30d11385"),
            crmSystemUserId: new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
            entraObjectId: new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static AgentKernelFactory CreateFactory(
        Mock<IAgentPeriodSetGetFunc>? periodFunc = null,
        Mock<IAgentTimesheetSetGetFunc>? timesheetFunc = null)
        =>
        new(
            (timesheetFunc ?? new()).Object,
            new Mock<IAgentProjectSetSearchFunc>().Object,
            new Mock<IAgentLastProjectSetGetFunc>().Object,
            (periodFunc ?? new()).Object,
            new Mock<IAgentTagSetGetFunc>().Object,
            TokenCredential.Object,
            SomeOption);
}

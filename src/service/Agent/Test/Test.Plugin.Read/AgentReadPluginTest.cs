using System;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentReadPluginTest
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

    private static AgentReadPlugin CreatePlugin(
        Mock<IAgentTimesheetSetGetFunc>? timesheetFunc = null,
        Mock<IAgentProjectSetSearchFunc>? projectSearchFunc = null,
        Mock<IAgentLastProjectSetGetFunc>? lastProjectFunc = null,
        Mock<IAgentPeriodSetGetFunc>? periodFunc = null,
        Mock<IAgentTagSetGetFunc>? tagFunc = null)
        =>
        new(
            SomeContext,
            (timesheetFunc ?? new()).Object,
            (projectSearchFunc ?? new()).Object,
            (lastProjectFunc ?? new()).Object,
            (periodFunc ?? new()).Object,
            (tagFunc ?? new()).Object);
}

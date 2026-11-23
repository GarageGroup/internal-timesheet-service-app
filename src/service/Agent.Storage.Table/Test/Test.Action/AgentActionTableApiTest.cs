using System;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

public static partial class AgentActionTableApiTest
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

    private static readonly AgentTimesheetCreateAction SomeAction
        =
        new(
            actionId: new("84e6c5b8-1597-4a2e-821a-f392c8c0ae3d"),
            date: new(2026, 09, 29),
            projectId: new("d9cb8306-dd0c-499b-ad90-44b9a324e30c"),
            projectName: "Some project",
            projectType: ProjectType.Project,
            duration: 1.5m,
            description: "Some description",
            createdAt: new(2026, 09, 29, 12, 00, 00, TimeSpan.Zero),
            expiresAt: new(2026, 09, 29, 12, 10, 00, TimeSpan.Zero));

    private static readonly AgentTimesheetDeleteAction SomeDeleteAction
        =
        new(
            actionId: new("2c596490-1917-428c-88b9-3e8dc55f835a"),
            timesheetId: new("46606dc6-335f-4271-86b7-ff9540e9f480"),
            date: new(2026, 09, 30),
            projectName: "Another project",
            duration: 2.25m,
            description: "Another description",
            createdAt: new(2026, 09, 30, 13, 00, 00, TimeSpan.Zero),
            expiresAt: new(2026, 09, 30, 13, 10, 00, TimeSpan.Zero));

    private static readonly AgentTimesheetUpdateAction SomeUpdateAction
        =
        new(
            actionId: new("da7d99f3-939c-4842-94f0-329157a061c8"),
            timesheetId: new("53478fc6-5d80-4148-9b93-f6b65457cf6d"),
            date: new(2026, 09, 28),
            projectId: new("7712b133-f72f-4b52-a0cc-78d0882ba84f"),
            projectName: "Updated project",
            projectType: ProjectType.Incident,
            duration: 3.75m,
            description: "Updated description",
            createdAt: new(2026, 09, 30, 14, 00, 00, TimeSpan.Zero),
            expiresAt: new(2026, 09, 30, 14, 10, 00, TimeSpan.Zero));
}

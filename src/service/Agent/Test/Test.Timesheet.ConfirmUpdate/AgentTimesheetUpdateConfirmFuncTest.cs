using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static partial class AgentTimesheetUpdateConfirmFuncTest
{
    private static readonly AgentUserContext SomeContext = new(
        101, 202, 303,
        new("80ae312e-305b-49dd-a905-d39e30d11385"),
        new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
        new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static readonly Guid SomeActionId = new("78302d93-e6dc-4fd6-be63-2480c8984382");

    private static readonly DateTimeOffset SomeUtcNow = new(2026, 09, 30, 12, 00, 00, TimeSpan.Zero);

    private sealed class TestDateProvider : IDateProvider
    {
        public DateTimeOffset UtcNow => SomeUtcNow;

        public DateOnly Today => DateOnly.FromDateTime(SomeUtcNow.UtcDateTime);
    }
}

using System;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test;

internal static partial class TimesheetDeleteFuncSource
{
    public static TheoryData<TimesheetDeleteIn, DataverseEntityDeleteIn> InputTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("14c5b8f3-d6cc-45c2-91fa-f1a6256ef8ce"),
                    timesheetId: new("17bdba90-1161-4715-b4bf-b416200acc79")),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("17bdba90-1161-4715-b4bf-b416200acc79")))
                {
                    CallerObjectId = new("14c5b8f3-d6cc-45c2-91fa-f1a6256ef8ce")
                }
            },
            {
                new(
                    systemUserId: new("f8f3e3c7-a81f-4a52-9d4e-aa47d9e673d0"),
                    timesheetId: new("4835096d-03ef-4e30-abc1-77bcfe3a5d5f")),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("4835096d-03ef-4e30-abc1-77bcfe3a5d5f")))
                {
                    CallerObjectId = new("f8f3e3c7-a81f-4a52-9d4e-aa47d9e673d0")
                }
            },
            {
                default,
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(default))
                {
                    CallerObjectId = default(Guid)
                }
            }
        };
}
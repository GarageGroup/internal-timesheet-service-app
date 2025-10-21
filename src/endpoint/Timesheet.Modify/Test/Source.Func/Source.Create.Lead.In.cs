using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

internal static partial class TimesheetModifyFuncSource
{
    public static TheoryData<TimesheetCreateIn, DataverseEntityGetOut<LeadJson>, DataverseEntityCreateIn<TimesheetJson>> InputCreateLeadTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("ded7a0d5-33c8-4e02-affe-61559ef4d4ca"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Lead),
                    duration: 8,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        CompanyName = "Some company name",
                        Subject = "Some subject"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        ChannelCode = 140120000,
                        Subject = "Some subject (Some company name)",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
                {
                    CallerObjectId = new("ded7a0d5-33c8-4e02-affe-61559ef4d4ca")
                }
            },
            {
                new(
                    systemUserId: new("cede85e3-d0db-44d3-8728-ce42549eb4d0"),
                    date: new(2023, 01, 12),
                    project: new(
                        id: new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        type: ProjectType.Lead),
                    duration: 3,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        CompanyName = null,
                        Subject = "Some subject"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2023, 01, 12),
                        Description = "Some message!",
                        Duration = 3,
                        ChannelCode = 140120000,
                        Subject = "Some subject",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(8829deda-5249-4412-9be5-ef5728fb928d)"
                        }
                    })
                {
                    CallerObjectId = new("cede85e3-d0db-44d3-8728-ce42549eb4d0")
                }
            },
            {
                new(
                    systemUserId: new("cede85e3-d0db-44d3-8728-ce42549eb4d0"),
                    date: new(2023, 01, 12),
                    project: new(
                        id: new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        type: ProjectType.Lead),
                    duration: 3,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        CompanyName = "Some company name",
                        Subject = null
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2023, 01, 12),
                        Description = "Some message!",
                        Duration = 3,
                        ChannelCode = 140120000,
                        Subject = "(Some company name)",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(8829deda-5249-4412-9be5-ef5728fb928d)"
                        }
                    })
                {
                    CallerObjectId = new("cede85e3-d0db-44d3-8728-ce42549eb4d0")
                }
            }
        };
}
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

using TheoryData = TheoryData<TimesheetCreateIn, DataverseEntityGetOut<ProjectJson>, DataverseEntityCreateIn<TimesheetJson>>;

internal static partial class TimesheetModifyFuncSource
{
    public static TheoryData InputCreateProjectTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("ded7a0d5-33c8-4e02-affe-61559ef4d4ca"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Project),
                    duration: 8,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        ProjectName = "Some project name"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        ChannelCode = 140120000,
                        Subject = "Some project name",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
                        type: ProjectType.Project),
                    duration: 3,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        ProjectName = null
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2023, 01, 12),
                        Description = "Some message!",
                        Duration = 3,
                        ChannelCode = 140120000,
                        Subject = null,
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(8829deda-5249-4412-9be5-ef5728fb928d)"
                        }
                    })
                {
                    CallerObjectId = new("cede85e3-d0db-44d3-8728-ce42549eb4d0")
                }
            },
            {
                new(
                    systemUserId: new("ce3e2f48-8eec-40f0-bb8b-60b8861a61cd"),
                    date: new(2023, 11, 03),
                    project: new(
                        id: new("13f0cb5c-b251-494c-9cae-1b0708471c10"),
                        type: ProjectType.Project),
                    duration: 15,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("13f0cb5c-b251-494c-9cae-1b0708471c10"),
                        ProjectName = string.Empty
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2023, 11, 03),
                        Description = "Some message!",
                        Duration = 15,
                        ChannelCode = 140120000,
                        Subject = string.Empty,
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(13f0cb5c-b251-494c-9cae-1b0708471c10)"
                        }
                    })
                {
                    CallerObjectId = new("ce3e2f48-8eec-40f0-bb8b-60b8861a61cd")
                }
            },
            {
                new(
                    systemUserId: new("c59436f5-709c-45aa-8469-5e79412f5108"),
                    date: new(2022, 12, 25),
                    project: new(
                        id: new("ca012870-a0f9-4945-a314-a14ebf690574"),
                        type: ProjectType.Project),
                    duration: -3,
                    description: "Some message!"),
                new(
                    new()
                    {
                        Id = new("ca012870-a0f9-4945-a314-a14ebf690574"),
                        ProjectName = "\r\n"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new()
                    {
                        Date = new(2022, 12, 25),
                        Description = "Some message!",
                        Duration = -3,
                        ChannelCode = 140120000,
                        Subject = "\r\n",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(ca012870-a0f9-4945-a314-a14ebf690574)"
                        }
                    })
                {
                    CallerObjectId = new("c59436f5-709c-45aa-8469-5e79412f5108")
                }
            }
        };
}
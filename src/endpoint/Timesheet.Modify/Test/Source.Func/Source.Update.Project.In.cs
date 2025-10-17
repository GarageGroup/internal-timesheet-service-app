using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

using DataverseProjectGetOut = DataverseEntityGetOut<ProjectJson>;

internal static partial class TimesheetModifyFuncSource
{
    public static TheoryData<TimesheetUpdateIn, DataverseProjectGetOut, DataverseEntityUpdateIn<TimesheetJson>> InputUpdateProjectTestData
        =>
        new()
        {
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
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
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        Subject = "Some project name",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: null,
                    project: new(
                        id: new("34990bca-f251-401d-aa80-45151ebd5211"),
                        type: ProjectType.Project),
                    duration: 8,
                    description: string.Empty),
                new(
                    new()
                    {
                        Id = new("ab8405ac-b9df-4710-86b4-3337fed27c09"),
                        ProjectName = null
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = null,
                        Description = null,
                        Duration = 8,
                        Subject = null,
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(ab8405ac-b9df-4710-86b4-3337fed27c09)"
                        }
                    })
            },
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Project),
                    duration: 8,
                    description: null),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        ProjectName = "\n\r"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = null,
                        Duration = 8,
                        Subject = "\n\r",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Project),
                    duration: null,
                    description: "\n\r"),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        ProjectName = string.Empty
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = null,
                        Duration = null,
                        Subject = string.Empty,
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: null,
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Project),
                    duration: null,
                    description: null),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        ProjectName = "Some project name"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = null,
                        Description = null,
                        Duration = null,
                        Subject = "Some project name",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: null,
                    duration: 8,
                    description: "Some message!"),
                new(default),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        Subject = null,
                        ExtensionData = null
                    })
            },
        };
}
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

using TheoryData = TheoryData<TimesheetUpdateIn, DataverseEntityGetOut<OpportunityJson>, DataverseEntityUpdateIn<TimesheetJson>>;

internal static partial class TimesheetModifyFuncSource
{
    public static TheoryData InputUpdateOpportunityTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Opportunity),
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
                            ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: null,
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Opportunity),
                    duration: 8,
                    description: string.Empty),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
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
                            ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Opportunity),
                    duration: 8,
                    description: "\n\r"),
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
                            ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: ProjectType.Opportunity),
                    duration: null,
                    description: "Some message!"),
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
                        Description = "Some message!",
                        Duration = null,
                        Subject = string.Empty,
                        ExtensionData = new()
                        {
                            ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(7583b4e6-23f5-eb11-94ef-00224884a588)"
                        }
                    })
            },
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: null,
                    project: new(
                        id: new("b6d557ef-8843-47b5-b169-42904b47fca8"),
                        type: ProjectType.Opportunity),
                    duration: null,
                    description: null),
                new(
                    new()
                    {
                        Id = new("221aedd2-5e2f-422e-acfb-e1099c126958"),
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
                            ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(221aedd2-5e2f-422e-acfb-e1099c126958)"
                        }
                    })
            },
            {
                new(
                    systemUserId: new("c34566c5-147a-420f-a786-0597b6e24333"),
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
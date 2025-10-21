using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

internal static partial class TimesheetModifyFuncSource
{
    public static TheoryData<TimesheetUpdateIn, DataverseEntityGetOut<LeadJson>, DataverseEntityUpdateIn<TimesheetJson>> InputUpdateLeadTestData
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
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        Subject = "Some subject (Some company name)",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
                        type: ProjectType.Lead),
                    duration: 8,
                    description: string.Empty),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        CompanyName = null,
                        Subject = "Some subject"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = null,
                        Description = null,
                        Duration = 8,
                        Subject = "Some subject",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
                        type: ProjectType.Lead),
                    duration: 8,
                    description: "\n\r"),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        CompanyName = string.Empty,
                        Subject = "Some subject"
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = null,
                        Duration = 8,
                        Subject = "Some subject",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
                        type: ProjectType.Lead),
                    duration: null,
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
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = null,
                        Subject = "Some subject (Some company name)",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
                        type: ProjectType.Lead),
                    duration: null,
                    description: null),
                new(
                    new()
                    {
                        Id = new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        CompanyName = "Some company name",
                        Subject = null
                    }),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = null,
                        Description = null,
                        Duration = null,
                        Subject = "(Some company name)",
                        ExtensionData = new()
                        {
                            ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
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
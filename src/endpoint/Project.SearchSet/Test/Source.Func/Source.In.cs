using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SearchSet.Test;

internal static partial class ProjectSetSearchFuncSource
{
    public static TheoryData<ProjectSetSearchIn, DataverseSearchIn> InputTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("4812cb4b-3049-4f84-bfb6-aac8f0425028"),
                    searchText: null,
                    top: 3),
                new("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Filter = "statecode eq 0",
                    Top = 3,
                    CallerObjectId = new("4812cb4b-3049-4f84-bfb6-aac8f0425028")
                }
            },
            {
                new(
                    systemUserId: new("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    searchText: string.Empty,
                    top: -2),
                new("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Filter = "statecode eq 0",
                    Top = -2,
                    CallerObjectId = new("32b49b76-01ca-4312-b7d5-499cf3addc22")
                }
            },
            {
                new(
                    systemUserId: new("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    searchText: "Some text",
                    top: null),
                new("*Some text*")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Filter = "statecode eq 0",
                    Top = null,
                    CallerObjectId = new("32b49b76-01ca-4312-b7d5-499cf3addc22")
                }
            }
        };
}
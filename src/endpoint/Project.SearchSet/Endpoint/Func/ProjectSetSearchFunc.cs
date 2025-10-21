using System;
using System.Linq;
using System.Text;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class ProjectSetSearchFunc(IDataverseSearchSupplier dataverseApi) : IProjectSetSearchFunc
{
    private const string ProjectEntityName = "gg_project";

    private const string LeadEntityName = "lead";

    private const string OpportunityEntityName = "opportunity";

    private const string IncidentEntityName = "incident";

    private static readonly FlatArray<string> EntityNames
        =
        [ProjectEntityName, LeadEntityName, OpportunityEntityName, IncidentEntityName];

    private static readonly string IsActiveFilter
        = 
        $"statecode eq 0";

    private static ProjectType? GetProjectType(DataverseSearchItem item)
        =>
        item.EntityName switch
        {
            ProjectEntityName => ProjectType.Project,
            LeadEntityName => ProjectType.Lead,
            OpportunityEntityName => ProjectType.Opportunity,
            IncidentEntityName => ProjectType.Incident,
            _ => null
        };

    private static string? GetProjectName(DataverseSearchItem item, ProjectType projectType)
        =>
        projectType switch
        {
            ProjectType.Project => item.ExtensionData.AsEnumerable().GetValueOrAbsent("gg_name").OrDefault()?.ToString(),
            ProjectType.Lead => BuildLeadProjectName(item),
            ProjectType.Opportunity => item.ExtensionData.AsEnumerable().GetValueOrAbsent("name").OrDefault()?.ToString(),
            _ => item.ExtensionData.AsEnumerable().GetValueOrAbsent("title").OrDefault()?.ToString()
        };

    private static string? BuildLeadProjectName(DataverseSearchItem item)
    {
        var companyName = item.ExtensionData.AsEnumerable().GetValueOrAbsent("companyname").OrDefault()?.ToString();
        var subject = item.ExtensionData.AsEnumerable().GetValueOrAbsent("subject").OrDefault()?.ToString();

        if (string.IsNullOrEmpty(companyName))
        {
            return subject;
        }

        var builder = new StringBuilder(subject);
        if (string.IsNullOrEmpty(subject) is false)
        {
            builder = builder.Append(' ');
        }

        return builder.Append('(').Append(companyName).Append(')').ToString();
    }
}
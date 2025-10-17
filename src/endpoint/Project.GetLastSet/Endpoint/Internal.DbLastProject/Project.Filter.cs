using GarageGroup.Infra;
using System;
using System.Linq;

namespace GarageGroup.Internal.Timesheet;

partial record class DbLastProject
{
    internal static DbRawFilter BuildIncidentStateCodeFilter()
        =>
        new(
            $"({AliasName}.regardingobjecttypecode = {ProjectType.Incident:D} " +
            $"AND EXISTS (SELECT TOP 1 1 FROM incident AS i WHERE {AliasName}.regardingobjectid = i.incidentid AND i.statecode = 0))");

    internal static DbRawFilter BuildLeadStateCodeFilter()
        =>
        new(
            $"({AliasName}.regardingobjecttypecode = {ProjectType.Lead:D} " +
            $"AND EXISTS (SELECT TOP 1 1 FROM lead AS l WHERE {AliasName}.regardingobjectid = l.leadid AND l.statecode = 0))");

    internal static DbRawFilter BuildOpportunityStateCodeFilter()
        =>
        new(
            $"({AliasName}.regardingobjecttypecode = {ProjectType.Opportunity:D} " +
            $"AND EXISTS (SELECT TOP 1 1 FROM opportunity AS o WHERE {AliasName}.regardingobjectid = o.opportunityid AND o.statecode = 0))");

    internal static DbRawFilter BuildProjectStateCodeFilter()
        =>
        new($"({ProjectAlias}.statecode = 0)");

    internal static DbParameterArrayFilter BuildAllowedProjectTypeSetFilter()
    {
        return new(
            fieldName: ProjectTypeCodeFieldName,
            @operator: DbArrayFilterOperator.In,
            fieldValues: Enum.GetValues<ProjectType>().Select(AsInt32).OrderBy(Pipeline.Pipe).Select(AsObject).ToFlatArray(),
            parameterPrefix: "projectTypeCode");

        static int AsInt32(ProjectType type)
            =>
            (int)type;

        static object? AsObject(int type)
            =>
            type;
    }

    internal static DbExistsFilter BuildOwnerFilter(Guid ownerId)
        =>
        new(
            selectQuery: new("systemuser", "u")
            {
                Top = 1,
                SelectedFields = new("1"),
                Filter = new DbCombinedFilter(DbLogicalOperator.And)
                {
                    Filters =
                    [
                        new DbRawFilter($"{AliasName}.ownerid = u.systemuserid"),
                        new DbParameterFilter("u.azureactivedirectoryobjectid", DbFilterOperator.Equal, ownerId, "ownerId")
                    ]
                }
            });

    internal static DbParameterFilter BuildMinDateFilter(DateOnly minDate)
        =>
        new($"{AliasName}.gg_date", DbFilterOperator.GreaterOrEqual, minDate.ToString("yyyy-MM-dd"), "minDate");
}

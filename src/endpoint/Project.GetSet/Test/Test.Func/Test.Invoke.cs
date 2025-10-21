using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SetGet.Test;

partial class ProjectSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectDbProjectSetGetCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(new(2024, 05, 30));
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var input = new ProjectSetGetIn(
            systemUserId: new("fea3adc6-699e-4847-a3f9-7b93bd45c14f"));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedProjectQuery = new DbSelectQuery("gg_project", "p")
        {
            SelectedFields =
            [
                "p.gg_projectid AS ProjectId",
                "p.gg_name AS ProjectName",
                "p.gg_comment AS ProjectComment",
                "user_last_timesheet_date.LastDay AS UserLastTimesheetDate",
                "last_timesheet_date.LastDay AS LastTimesheetDate"
            ],
            Filter = new DbRawFilter("p.statecode = 0"),
            AppliedTables = 
            [
                new(
                type: DbApplyType.Outer,
                alias: "user_last_timesheet_date",
                selectQuery: new("gg_timesheetactivity", "t")
                {
                    Top = 1,
                    SelectedFields = new("t.gg_date AS LastDay"),
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                    {
                        Filters =
                        [
                            new DbExistsFilter(
                                selectQuery: new DbSelectQuery("systemuser", "u")
                                {
                                    Top = 1,
                                    SelectedFields = new("1"),
                                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                                    {
                                        Filters =
                                        [
                                            new DbRawFilter("t.ownerid = u.systemuserid"),
                                            new DbParameterFilter(
                                                fieldName: "u.azureactivedirectoryobjectid",
                                                @operator: DbFilterOperator.Equal,
                                                fieldValue: Guid.Parse("fea3adc6-699e-4847-a3f9-7b93bd45c14f"),
                                                parameterName: "userId")
                                        ]
                                    }
                                }),
                            new DbParameterFilter(
                                "t.gg_date", DbFilterOperator.GreaterOrEqual, "2024-04-30", "minDate"),
                            new DbRawFilter(
                                "p.gg_projectid = t.regardingobjectid"),
                            new DbRawFilter(
                                "t.regardingobjecttypecode = 10912"),
                            new DbRawFilter(
                                "t.statecode = 0")
                        ]
                    },
                    Orders =
                    [
                        new("t.gg_date", DbOrderType.Descending)
                    ]
                }),
            new(
                type: DbApplyType.Outer,
                alias: "last_timesheet_date",
                selectQuery: new("gg_timesheetactivity", "t1")
                {
                    Top = 1,
                    SelectedFields = new("t1.gg_date AS LastDay"),
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                    {
                        Filters =
                        [
                            new DbExistsFilter(
                                selectQuery: new DbSelectQuery("systemuser", "u")
                                {
                                    Top = 1,
                                    SelectedFields = new("1"),
                                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                                    {
                                        Filters =
                                        [
                                            new DbRawFilter("t1.ownerid = u.systemuserid"),
                                            new DbParameterFilter(
                                                fieldName: "u.azureactivedirectoryobjectid",
                                                @operator: DbFilterOperator.Equal,
                                                fieldValue: Guid.Parse("fea3adc6-699e-4847-a3f9-7b93bd45c14f"),
                                                parameterName: "userId")
                                        ]
                                    }
                                }),
                            new DbParameterFilter(
                                "t1.gg_date", DbFilterOperator.GreaterOrEqual, "2024-04-30", "minDate"),
                            new DbRawFilter(
                                "p.gg_projectid = t1.regardingobjectid"),
                            new DbRawFilter(
                                "t1.regardingobjecttypecode = 10912"),
                            new DbRawFilter(
                                "t1.statecode = 0")
                        ]
                    },
                    Orders =
                    [
                        new("t1.gg_date", DbOrderType.Descending)
                    ]
                })
            ]
        };

        mockSqlApi.Verify(
            a => a.QueryEntitySetOrFailureAsync<DbProject>(expectedProjectQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbProjectSetGetResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some Failure message");

        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, dbFailure, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_ExpectDbOpportunitySetGetCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expectedOpportunityQuery = new DbSelectQuery("opportunity", "o")
        {
            SelectedFields =
            [
                "o.opportunityid AS ProjectId",
                "o.name AS ProjectName"
            ],
            Filter = new DbRawFilter("o.statecode = 0")
        };

        mockSqlApi.Verify(
            a => a.QueryEntitySetOrFailureAsync<DbOpportunity>(expectedOpportunityQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbOpportunitySetGetResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some Failure message");

        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, dbFailure, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_ExpectDbLeadSetGetCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expectedLeadQuery = new DbSelectQuery("lead", "l")
        {
            SelectedFields =
            [
                "l.leadid AS ProjectId",
                "l.companyname AS CompanyName",
                "l.subject AS Subject"
            ],
            Filter = new DbRawFilter("l.statecode = 0")
        };

        mockSqlApi.Verify(
            a => a.QueryEntitySetOrFailureAsync<DbLead>(expectedLeadQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbLeadSetGetResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some Failure message");

        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, dbFailure);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_ExpectDbIncidentSetGetCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expectedIncidentQuery = new DbSelectQuery("incident", "i")
        {
            SelectedFields =
            [
                "i.incidentid AS ProjectId",
                "i.title AS ProjectName"
            ],
            Filter = new DbRawFilter("i.statecode = 0")
        };

        mockSqlApi.Verify(
            a => a.QueryEntitySetOrFailureAsync<DbIncident>(expectedIncidentQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbIncidentSetGetResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some Failure message");

        var mockSqlApi = BuildMockSqlApi(dbFailure, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ProjectSetGetFuncSource.OutputGetTestData), MemberType = typeof(ProjectSetGetFuncSource))]
    internal static async Task InvokeAsync_DbResultIsSuccess_ExpectSuccess(
        FlatArray<DbIncident> dbIncidents,
        FlatArray<DbProject> dbProjects,
        FlatArray<DbOpportunity> dbOpportunities,
        FlatArray<DbLead> dbLeads,
        ProjectSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi(dbIncidents, dbProjects, dbOpportunities, dbLeads);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        Assert.StrictEqual(expected, actual);
    }
}

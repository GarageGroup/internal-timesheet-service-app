using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class ProjectSetGetFunc
{
    public ValueTask<Result<ProjectSetGetOut, Failure<Unit>>> InvokeAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeParallelValue(
            GetProjectsAsync,
            GetIncidentsAsync,
            GetOpportunitiesAsync,
            GetLeadsAsync)
        .MapSuccess(
            @out => new ProjectSetGetOut
            {
                Projects = ConcatProjects(@out.Item1, @out.Item2, @out.Item3, @out.Item4, input.Top)
            });

    private ValueTask<Result<FlatArray<DbProject>, Failure<Unit>>> GetProjectsAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            @in => DbProject.QueryAll with
            {
                Filter = DbProject.IsActiveFilter,
                AppliedTables = DbProject.BuildTimesheetDateDbAppliedTables(@in.SystemUserId, todayProvider.Today.AddDays(-LastDaysPeriod))
            })
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbProject>);

    private ValueTask<Result<FlatArray<DbOpportunity>, Failure<Unit>>> GetOpportunitiesAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            DbOpportunityQueryAll, cancellationToken)
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbOpportunity>);

    private ValueTask<Result<FlatArray<DbLead>, Failure<Unit>>> GetLeadsAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            DbLeadQueryAll, cancellationToken)
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbLead>);

    private ValueTask<Result<FlatArray<DbIncident>, Failure<Unit>>> GetIncidentsAsync(
        ProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            DbIncidentQueryAll, cancellationToken)
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbIncident>);

    private static FlatArray<ProjectItem> ConcatProjects(
        FlatArray<DbProject> projects,
        FlatArray<DbIncident> incidents,
        FlatArray<DbOpportunity> opportunities,
        FlatArray<DbLead> leads,
        int? top)
        =>
        Pipeline.Pipe(
            projects.CastArray<IDbProject>().AsEnumerable())
        .Concat(
            opportunities.CastArray<IDbProject>().AsEnumerable()
        .Concat(
            leads.CastArray<IDbProject>().AsEnumerable())
        .Concat(
            incidents.CastArray<IDbProject>().AsEnumerable()))
        .ToArray()
        .OrderByDescending(
            GetUserLastTimesheetDate)
        .ThenByDescending(
            GetLastTimesheetDate)
        .ThenBy(
            GetProjectName)
        .Take(
            top > 0 ? top.Value : int.MaxValue)
        .Select(
            MapProject)
        .ToFlatArray();
}
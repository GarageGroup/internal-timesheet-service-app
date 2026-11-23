using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetUpdatePrepareFunc
{
    public ValueTask<Result<AgentTimesheetUpdatePrepareOut, Failure<AgentTimesheetUpdatePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetUpdatePrepareIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .ForwardValue(
            (@in, token) => PrepareAsync(context, @in, token));

    private static Result<AgentTimesheetUpdatePrepareIn, Failure<AgentTimesheetUpdatePrepareFailureCode>> ValidateInput(
        AgentTimesheetUpdatePrepareIn input)
    {
        if (input.TimesheetId == Guid.Empty)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.InvalidTimesheet, "Timesheet ID is empty");
        }

        if (input.Duration is <= 0)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.InvalidDuration, "Duration must be greater than zero");
        }

        if (input.Date is null && input.ProjectId is null && input.Duration is null && input.Description is null)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.EmptyChanges, "No changes were specified");
        }

        return input;
    }

    private async ValueTask<Result<AgentTimesheetUpdatePrepareOut, Failure<AgentTimesheetUpdatePrepareFailureCode>>> PrepareAsync(
        AgentUserContext context,
        AgentTimesheetUpdatePrepareIn input,
        CancellationToken cancellationToken)
    {
        var timesheetSetResult = await timesheetSetGetFunc.InvokeAsync(
            context,
            new(input.SourceDate, input.SourceDate),
            cancellationToken).ConfigureAwait(false);

        if (timesheetSetResult.IsFailure)
        {
            return timesheetSetResult.FailureOrThrow().WithFailureCode(AgentTimesheetUpdatePrepareFailureCode.Unknown);
        }

        var timesheet = timesheetSetResult.SuccessOrThrow().Timesheets.AsEnumerable().FirstOrDefault(
            item => item.Id == input.TimesheetId && item.Date == input.SourceDate);

        if (timesheet is null)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.NotFound, "Timesheet is not available");
        }

        if (timesheet.IsActive is false)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.ReadOnly, "Timesheet is read-only");
        }

        var projectResult = await GetProjectAsync(context, timesheet, input.ProjectId, cancellationToken).ConfigureAwait(false);
        if (projectResult.IsFailure)
        {
            return projectResult.FailureOrThrow();
        }

        var project = projectResult.SuccessOrThrow();
        var date = input.Date ?? timesheet.Date;
        var duration = input.Duration ?? timesheet.Duration;
        var description = input.Description ?? timesheet.Description;
        if (date == timesheet.Date &&
            project.Id == timesheet.ProjectId &&
            duration == timesheet.Duration &&
            string.Equals(description, timesheet.Description, StringComparison.Ordinal))
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.EmptyChanges, "Specified values do not change the timesheet");
        }

        var createdAt = dateProvider.UtcNow;
        var action = new AgentTimesheetUpdateAction(
            Guid.NewGuid(),
            timesheet.Id,
            date,
            project.Id,
            project.Name,
            project.Type,
            duration,
            description,
            createdAt,
            createdAt.Add(option.ApprovalTtl));

        var storeResult = await actionStore.CreateAsync(context, action, cancellationToken).ConfigureAwait(false);
        if (storeResult.IsFailure)
        {
            return storeResult.FailureOrThrow().FailureCode is AgentActionStoreFailureCode.Conflict
                ? Failure.Create(AgentTimesheetUpdatePrepareFailureCode.Conflict, "Agent action already exists")
                : storeResult.FailureOrThrow().WithFailureCode(AgentTimesheetUpdatePrepareFailureCode.Unknown);
        }

        return new AgentTimesheetUpdatePrepareOut(
            action.ActionId,
            action.TimesheetId,
            action.Date,
            action.ProjectId,
            action.ProjectName,
            action.ProjectType,
            action.Duration,
            action.Description,
            action.ExpiresAt);
    }

    private async ValueTask<Result<ResolvedProject, Failure<AgentTimesheetUpdatePrepareFailureCode>>> GetProjectAsync(
        AgentUserContext context,
        AgentTimesheetSetGetItem timesheet,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        if (projectId is null || projectId == timesheet.ProjectId)
        {
            return new ResolvedProject(timesheet.ProjectId, timesheet.ProjectName, timesheet.ProjectType);
        }

        var projectSetResult = await projectSetGetFunc.InvokeAsync(
            new(context.EntraObjectId),
            cancellationToken).ConfigureAwait(false);
        if (projectSetResult.IsFailure)
        {
            return Failure.Create(AgentTimesheetUpdatePrepareFailureCode.Unknown, "Failed to get available projects");
        }

        var project = projectSetResult.SuccessOrThrow().Projects.AsEnumerable().FirstOrDefault(
            item => item.Id == projectId);

        return project is null
            ? Failure.Create(AgentTimesheetUpdatePrepareFailureCode.InvalidProject, "Project is not available")
            : new ResolvedProject(project.Id, project.Name, project.Type);
    }

    private readonly record struct ResolvedProject(Guid Id, string Name, ProjectType Type);
}

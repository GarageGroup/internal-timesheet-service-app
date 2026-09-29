using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetCreatePrepareFunc
{
    public ValueTask<Result<AgentTimesheetCreatePrepareOut, Failure<AgentTimesheetCreatePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetCreatePrepareIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .ForwardValue(
            (@in, token) => PrepareAsync(context, @in, token));

    private static Result<AgentTimesheetCreatePrepareIn, Failure<AgentTimesheetCreatePrepareFailureCode>> ValidateInput(
        AgentTimesheetCreatePrepareIn input)
    {
        if (input.ProjectId == Guid.Empty ||
            string.IsNullOrWhiteSpace(input.ProjectName) ||
            Enum.IsDefined(input.ProjectType) is false)
        {
            return Failure.Create(AgentTimesheetCreatePrepareFailureCode.InvalidProject, "Project is invalid");
        }

        if (input.Duration <= 0)
        {
            return Failure.Create(AgentTimesheetCreatePrepareFailureCode.InvalidDuration, "Duration must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return Failure.Create(AgentTimesheetCreatePrepareFailureCode.EmptyDescription, "Description is empty");
        }

        return input;
    }

    private async ValueTask<Result<AgentTimesheetCreatePrepareOut, Failure<AgentTimesheetCreatePrepareFailureCode>>> PrepareAsync(
        AgentUserContext context,
        AgentTimesheetCreatePrepareIn input,
        CancellationToken cancellationToken)
    {
        var projectSetResult = await projectSetSearchFunc.InvokeAsync(
            context,
            new(input.ProjectName, option.ProjectSearchTop),
            cancellationToken).ConfigureAwait(false);

        if (projectSetResult.IsFailure)
        {
            return projectSetResult.FailureOrThrow().MapFailureCode(MapProjectFailureCode);
        }

        var project = projectSetResult.SuccessOrThrow().Projects.AsEnumerable().FirstOrDefault(
            project => project.Id == input.ProjectId && project.Type == input.ProjectType);

        if (project is null)
        {
            return Failure.Create(AgentTimesheetCreatePrepareFailureCode.InvalidProject, "Project is not available");
        }

        var createdAt = dateProvider.UtcNow;
        var action = new AgentTimesheetCreateAction(
            Guid.NewGuid(),
            input.Date,
            project.Id,
            project.Name,
            project.Type,
            input.Duration,
            input.Description,
            createdAt,
            createdAt.Add(option.ApprovalTtl));

        var storeResult = await actionStore.CreateAsync(context, action, cancellationToken).ConfigureAwait(false);
        if (storeResult.IsFailure)
        {
            return storeResult.FailureOrThrow().MapFailureCode(MapStoreFailureCode);
        }

        return new AgentTimesheetCreatePrepareOut(
            action.ActionId,
            action.Date,
            action.ProjectId,
            action.ProjectName,
            action.ProjectType,
            action.Duration,
            action.Description.OrEmpty(),
            action.ExpiresAt);
    }

    private static AgentTimesheetCreatePrepareFailureCode MapProjectFailureCode(AgentProjectSetSearchFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentProjectSetSearchFailureCode.Forbidden => AgentTimesheetCreatePrepareFailureCode.Forbidden,
            _ => AgentTimesheetCreatePrepareFailureCode.Unknown
        };

    private static AgentTimesheetCreatePrepareFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetCreatePrepareFailureCode.Conflict,
            _ => AgentTimesheetCreatePrepareFailureCode.Unknown
        };
}

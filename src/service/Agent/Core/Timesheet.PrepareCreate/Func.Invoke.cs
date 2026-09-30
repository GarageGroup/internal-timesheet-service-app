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
        if (input.ProjectId == Guid.Empty)
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
        var projectSetResult = await projectSetGetFunc.InvokeAsync(
            new(context.EntraObjectId),
            cancellationToken).ConfigureAwait(false);

        if (projectSetResult.IsFailure)
        {
            return Failure.Create(AgentTimesheetCreatePrepareFailureCode.Unknown, "Failed to get available projects");
        }

        var project = projectSetResult.SuccessOrThrow().Projects.AsEnumerable().FirstOrDefault(
            project => project.Id == input.ProjectId);

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

    private static AgentTimesheetCreatePrepareFailureCode MapStoreFailureCode(AgentActionStoreFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentActionStoreFailureCode.Conflict => AgentTimesheetCreatePrepareFailureCode.Conflict,
            _ => AgentTimesheetCreatePrepareFailureCode.Unknown
        };
}

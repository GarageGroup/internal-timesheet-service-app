using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionDecideFunc
{
    public ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> InvokeAsync(
        AgentActionDecideIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .ForwardValue(
            (@in, token) => userContextResolver.ResolveAsync(
                new(@in.BotId, @in.TelegramUserId, @in.TelegramChatId),
                token),
            static failure => failure.MapFailureCode(MapUserFailureCode))
        .ForwardValue(
            (context, token) => DecideAsync(context, input, token));

    private Result<AgentActionDecideIn, Failure<AgentActionDecideFailureCode>> ValidateInput(
        AgentActionDecideIn input)
    {
        if (option.Enabled is false)
        {
            return Failure.Create(AgentActionDecideFailureCode.WriteDisabled, "Agent write operations are disabled");
        }

        if (input.ActionId == Guid.Empty ||
            input.TelegramUpdateId <= 0 ||
            Enum.IsDefined(input.Decision) is false)
        {
            return Failure.Create(AgentActionDecideFailureCode.InvalidDecision, "Agent action decision is invalid");
        }

        return input;
    }

    private ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> DecideAsync(
        AgentUserContext context,
        AgentActionDecideIn input,
        CancellationToken cancellationToken)
    {
        if (input.Decision is AgentActionDecision.Confirm)
        {
            return AsyncPipeline.Pipe(
                input.ActionId, cancellationToken)
                .PipeValue(
                    (actionId, token) => ConfirmAsync(context, actionId, token))
                .ForwardValue(
                    (date, token) => BuildConfirmedOutAsync(context, input, date, token));
        }

        return AsyncPipeline.Pipe(
            input.ActionId, cancellationToken)
            .PipeValue(
                (actionId, token) => CancelAsync(context, actionId, token))
            .MapSuccess(
                _ => new AgentActionDecideOut(input.ActionId, input.Decision));
    }

    private async ValueTask<Result<DateOnly, Failure<AgentActionDecideFailureCode>>> ConfirmAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
    {
        var createResult = await createConfirmFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (createResult.IsSuccess)
        {
            return createResult.SuccessOrThrow().Date;
        }

        var createFailure = createResult.FailureOrThrow();
        if (createFailure.FailureCode is not AgentTimesheetCreateConfirmFailureCode.NotFound)
        {
            return createFailure.MapFailureCode(MapCreateConfirmFailureCode);
        }

        var deleteResult = await deleteConfirmFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsSuccess)
        {
            return deleteResult.SuccessOrThrow().Date;
        }

        var deleteFailure = deleteResult.FailureOrThrow();
        if (deleteFailure.FailureCode is not AgentTimesheetDeleteConfirmFailureCode.NotFound)
        {
            return deleteFailure.MapFailureCode(MapDeleteConfirmFailureCode);
        }

        var updateResult = await updateConfirmFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        return updateResult.Map(
            static success => success.Date,
            static failure => failure.MapFailureCode(MapUpdateConfirmFailureCode));
    }

    private async ValueTask<Result<Unit, Failure<AgentActionDecideFailureCode>>> CancelAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
    {
        var createResult = await createCancelFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (createResult.IsSuccess)
        {
            return default(Unit);
        }

        var createFailure = createResult.FailureOrThrow();
        if (createFailure.FailureCode is not AgentTimesheetCreateCancelFailureCode.NotFound)
        {
            return createFailure.MapFailureCode(MapCreateCancelFailureCode);
        }

        var deleteResult = await deleteCancelFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsSuccess)
        {
            return default(Unit);
        }

        var deleteFailure = deleteResult.FailureOrThrow();
        if (deleteFailure.FailureCode is not AgentTimesheetDeleteCancelFailureCode.NotFound)
        {
            return deleteFailure.MapFailureCode(MapDeleteCancelFailureCode);
        }

        var updateResult = await updateCancelFunc.InvokeAsync(context, actionId, cancellationToken).ConfigureAwait(false);
        return updateResult.Map(
            static _ => default(Unit),
            static failure => failure.MapFailureCode(MapUpdateCancelFailureCode));
    }

    private async ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> BuildConfirmedOutAsync(
        AgentUserContext context,
        AgentActionDecideIn input,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var timesheetResult = await timesheetSetGetFunc.InvokeAsync(
            context,
            new(date, date),
            cancellationToken).ConfigureAwait(false);

        if (timesheetResult.IsFailure)
        {
            return new AgentActionDecideOut(input.ActionId, input.Decision)
            {
                Date = date
            };
        }

        return new AgentActionDecideOut(input.ActionId, input.Decision)
        {
            Date = date,
            TimesheetsLoaded = true,
            Timesheets = timesheetResult.SuccessOrThrow().Timesheets.AsEnumerable().Select(MapTimesheet).ToArray()
        };
    }

    private static AgentActionTimesheetOut MapTimesheet(AgentTimesheetSetGetItem item)
        =>
        new(
            item.Id,
            item.ProjectName,
            item.ProjectType.ToString(),
            item.Duration,
            item.Description,
            item.IsActive);

    private static AgentActionDecideFailureCode MapUserFailureCode(AgentUserContextResolveFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentUserContextResolveFailureCode.InvalidIdentity => AgentActionDecideFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UnsupportedChat => AgentActionDecideFailureCode.InvalidIdentity,
            AgentUserContextResolveFailureCode.UserNotLinked => AgentActionDecideFailureCode.UserNotLinked,
            AgentUserContextResolveFailureCode.AmbiguousBinding => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.BindingSignedOut => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.UserDisabled => AgentActionDecideFailureCode.UserUnavailable,
            AgentUserContextResolveFailureCode.MissingEntraObjectId => AgentActionDecideFailureCode.UserUnavailable,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapCreateConfirmFailureCode(AgentTimesheetCreateConfirmFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetCreateConfirmFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetCreateConfirmFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetCreateConfirmFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetCreateConfirmFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetCreateConfirmFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            AgentTimesheetCreateConfirmFailureCode.BadRequest => AgentActionDecideFailureCode.InvalidTimesheet,
            AgentTimesheetCreateConfirmFailureCode.Forbidden => AgentActionDecideFailureCode.TimesheetForbidden,
            AgentTimesheetCreateConfirmFailureCode.ProjectNotFound => AgentActionDecideFailureCode.ProjectNotFound,
            AgentTimesheetCreateConfirmFailureCode.Indeterminate => AgentActionDecideFailureCode.Indeterminate,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapCreateCancelFailureCode(AgentTimesheetCreateCancelFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetCreateCancelFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetCreateCancelFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetCreateCancelFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetCreateCancelFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetCreateCancelFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapDeleteConfirmFailureCode(AgentTimesheetDeleteConfirmFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetDeleteConfirmFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetDeleteConfirmFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetDeleteConfirmFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetDeleteConfirmFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetDeleteConfirmFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            AgentTimesheetDeleteConfirmFailureCode.BadRequest => AgentActionDecideFailureCode.InvalidTimesheet,
            AgentTimesheetDeleteConfirmFailureCode.Indeterminate => AgentActionDecideFailureCode.Indeterminate,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapDeleteCancelFailureCode(AgentTimesheetDeleteCancelFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetDeleteCancelFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetDeleteCancelFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetDeleteCancelFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetDeleteCancelFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetDeleteCancelFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapUpdateConfirmFailureCode(AgentTimesheetUpdateConfirmFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetUpdateConfirmFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetUpdateConfirmFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetUpdateConfirmFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetUpdateConfirmFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetUpdateConfirmFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            AgentTimesheetUpdateConfirmFailureCode.BadRequest => AgentActionDecideFailureCode.InvalidTimesheet,
            AgentTimesheetUpdateConfirmFailureCode.ProjectNotFound => AgentActionDecideFailureCode.ProjectNotFound,
            AgentTimesheetUpdateConfirmFailureCode.Indeterminate => AgentActionDecideFailureCode.Indeterminate,
            _ => AgentActionDecideFailureCode.Unknown
        };

    private static AgentActionDecideFailureCode MapUpdateCancelFailureCode(AgentTimesheetUpdateCancelFailureCode failureCode)
        =>
        failureCode switch
        {
            AgentTimesheetUpdateCancelFailureCode.InvalidActionId => AgentActionDecideFailureCode.InvalidDecision,
            AgentTimesheetUpdateCancelFailureCode.NotFound => AgentActionDecideFailureCode.ActionNotFound,
            AgentTimesheetUpdateCancelFailureCode.Expired => AgentActionDecideFailureCode.ActionExpired,
            AgentTimesheetUpdateCancelFailureCode.InvalidState => AgentActionDecideFailureCode.InvalidActionState,
            AgentTimesheetUpdateCancelFailureCode.Conflict => AgentActionDecideFailureCode.ActionConflict,
            _ => AgentActionDecideFailureCode.Unknown
        };
}

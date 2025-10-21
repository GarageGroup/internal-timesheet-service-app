using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetModifyFunc
{
    public ValueTask<Result<Unit, Failure<TimesheetCreateFailureCode>>> InvokeAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateDescription)
        .ForwardValue(
            (@in, token) => GetProjectAsync(@in.Project, @in.SystemUserId, token),
            static failure => failure.MapFailureCode(ToTimesheetCreateFailureCode))
        .MapSuccess(
            project => TimesheetJson.BuildDataverseCreateInput(
                timesheet: new(project)
                {
                    Date = input.Date,
                    Description = input.Description.OrNullIfEmpty(),
                    Duration = input.Duration,
                    ChannelCode = TelegramChannelCode
                },
                callerObjectId: input.SystemUserId))
        .ForwardValue(
            dataverseApi.CreateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetCreateFailureCode));

    private static Result<TimesheetCreateIn, Failure<TimesheetCreateFailureCode>> ValidateDescription(TimesheetCreateIn input)
    {
        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return Failure.Create(TimesheetCreateFailureCode.EmptyDescription, "Description is empty");
        }

        return input;
    }

    private static TimesheetCreateFailureCode ToTimesheetCreateFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.UserNotEnabled => TimesheetCreateFailureCode.Forbidden,
            DataverseFailureCode.PrivilegeDenied => TimesheetCreateFailureCode.Forbidden,
            _ => default
        };

    private static TimesheetCreateFailureCode ToTimesheetCreateFailureCode(ProjectNameFailureCode failureCode)
        =>
        failureCode switch
        {
            ProjectNameFailureCode.ProjectNotFound => TimesheetCreateFailureCode.ProjectNotFound,
            ProjectNameFailureCode.InvalidProject => TimesheetCreateFailureCode.UnexpectedProjectType,
            _ => default
        };
}
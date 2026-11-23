using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetSetGetFunc
{
    public ValueTask<Result<AgentTimesheetSetGetOut, Failure<AgentTimesheetSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetSetGetIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .MapSuccess(
            @in => new TimesheetSetGetIn(context.EntraObjectId, @in.DateFrom, @in.DateTo))
        .ForwardValue(
            timesheetSetGetFunc.InvokeAsync,
            static failure => failure.WithFailureCode(AgentTimesheetSetGetFailureCode.Unknown))
        .MapSuccess(
            static @out => new AgentTimesheetSetGetOut
            {
                Timesheets = @out.Timesheets.Map(MapTimesheet)
            });

    private Result<AgentTimesheetSetGetIn, Failure<AgentTimesheetSetGetFailureCode>> ValidateInput(
        AgentTimesheetSetGetIn input)
    {
        if (input.DateFrom > input.DateTo)
        {
            return Failure.Create(AgentTimesheetSetGetFailureCode.InvalidDateRange, "Date from must not be later than date to");
        }

        var dateRangeInDays = input.DateTo.DayNumber - input.DateFrom.DayNumber + 1;
        if (dateRangeInDays > option.MaxDateRangeInDays)
        {
            return Failure.Create(
                AgentTimesheetSetGetFailureCode.DateRangeTooLong,
                $"Date range must not exceed {option.MaxDateRangeInDays} days");
        }

        return input;
    }

    private static AgentTimesheetSetGetItem MapTimesheet(TimesheetSetGetItem item)
        =>
        new(
            id: item.Id,
            projectId: item.ProjectId,
            projectType: item.ProjectType,
            projectName: item.ProjectName,
            duration: item.Duration,
            description: item.Description,
            isActive: item.IsActive,
            date: item.Date)
        {
            ProjectComment = item.ProjectComment
        };
}

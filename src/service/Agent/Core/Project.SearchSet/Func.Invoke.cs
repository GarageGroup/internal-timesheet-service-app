using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentProjectSetSearchFunc
{
    public ValueTask<Result<AgentProjectSetSearchOut, Failure<AgentProjectSetSearchFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentProjectSetSearchIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .MapSuccess(
            @in => new ProjectSetSearchIn(context.EntraObjectId, @in.SearchText, @in.Top))
        .ForwardValue(
            projectSetSearchFunc.InvokeAsync,
            static failure => failure.MapFailureCode(MapFailureCode))
        .MapSuccess(
            static @out => new AgentProjectSetSearchOut
            {
                Projects = @out.Projects.Map(static project => new AgentProjectItem(project.Id, project.Name, project.Type))
            });

    private Result<AgentProjectSetSearchIn, Failure<AgentProjectSetSearchFailureCode>> ValidateInput(
        AgentProjectSetSearchIn input)
    {
        var searchText = input.SearchText.Trim();
        if (searchText.Length is 0)
        {
            return Failure.Create(AgentProjectSetSearchFailureCode.InvalidSearchText, "Search text is empty");
        }

        if (searchText.Length > option.MaxSearchTextLength)
        {
            return Failure.Create(
                AgentProjectSetSearchFailureCode.SearchTextTooLong,
                $"Search text must not exceed {option.MaxSearchTextLength} characters");
        }

        var top = input.Top ?? option.DefaultTop;
        if (top <= 0 || top > option.MaxTop)
        {
            return Failure.Create(
                AgentProjectSetSearchFailureCode.InvalidTop,
                $"Top must be between 1 and {option.MaxTop}");
        }

        return new AgentProjectSetSearchIn(searchText, top);
    }

    private static AgentProjectSetSearchFailureCode MapFailureCode(ProjectSetSearchFailureCode failureCode)
        =>
        failureCode switch
        {
            ProjectSetSearchFailureCode.Forbidden => AgentProjectSetSearchFailureCode.Forbidden,
            _ => AgentProjectSetSearchFailureCode.Unknown
        };
}

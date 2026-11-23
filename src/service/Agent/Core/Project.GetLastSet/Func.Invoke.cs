using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentLastProjectSetGetFunc
{
    public ValueTask<Result<AgentLastProjectSetGetOut, Failure<AgentLastProjectSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentLastProjectSetGetIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .MapSuccess(
            @in => new LastProjectSetGetIn(context.EntraObjectId, @in.Top))
        .ForwardValue(
            lastProjectSetGetFunc.InvokeAsync,
            static failure => failure.WithFailureCode(AgentLastProjectSetGetFailureCode.Unknown))
        .MapSuccess(
            static @out => new AgentLastProjectSetGetOut
            {
                Projects = @out.Projects.Map(
                    static project => new AgentProjectItem(project.Id, project.Name, project.Type)
                    {
                        Comment = project.Comment
                    })
            });

    private Result<AgentLastProjectSetGetIn, Failure<AgentLastProjectSetGetFailureCode>> ValidateInput(
        AgentLastProjectSetGetIn input)
    {
        var top = input.Top ?? option.DefaultTop;
        if (top <= 0 || top > option.MaxTop)
        {
            return Failure.Create(
                AgentLastProjectSetGetFailureCode.InvalidTop,
                $"Top must be between 1 and {option.MaxTop}");
        }

        return new AgentLastProjectSetGetIn(top);
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTagSetGetFunc
{
    public ValueTask<Result<AgentTagSetGetOut, Failure<AgentTagSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTagSetGetIn input,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .MapSuccess(
            @in => new TagSetGetIn(context.EntraObjectId, @in.ProjectId))
        .ForwardValue(
            tagSetGetFunc.InvokeAsync,
            static failure => failure.WithFailureCode(AgentTagSetGetFailureCode.Unknown))
        .MapSuccess(
            @out => new AgentTagSetGetOut
            {
                Tags = @out.Tags.AsEnumerable().Take(option.MaxTags).ToFlatArray()
            });

    private static Result<AgentTagSetGetIn, Failure<AgentTagSetGetFailureCode>> ValidateInput(
        AgentTagSetGetIn input)
    {
        if (input.ProjectId == Guid.Empty)
        {
            return Failure.Create(AgentTagSetGetFailureCode.InvalidProjectId, "Project ID is empty");
        }

        return input;
    }
}

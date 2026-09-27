using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentPeriodSetGetFunc
{
    public ValueTask<Result<AgentPeriodSetGetOut, Failure<AgentPeriodSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe<Unit>(
            default, cancellationToken)
        .PipeValue(
            periodSetGetFunc.InvokeAsync)
        .Map(
            static @out => new AgentPeriodSetGetOut
            {
                Periods = @out.Periods.Map(
                    static period => new AgentPeriodItem(period.Name, period.From, period.To))
            },
            static failure => failure.WithFailureCode(AgentPeriodSetGetFailureCode.Unknown));
}

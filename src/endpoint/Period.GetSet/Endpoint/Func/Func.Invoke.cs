using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class PeriodSetGetFunc
{
    public ValueTask<Result<PeriodSetGetOut, Failure<Unit>>> InvokeAsync(
        Unit input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            PeriodJson.DataverseSetGetInput, cancellationToken)
        .PipeValue(
            dataverseApi.GetEntitySetAsync<PeriodJson>)
        .Map(
            static @out => new PeriodSetGetOut
            {
                Periods = @out.Value.Map(MapPeriod)
            },
            static failure => failure.WithFailureCode<Unit>(default));

    private static PeriodItem MapPeriod(PeriodJson period)
        =>
        new(
            name: period.Name,
            dateFrom: DateOnly.FromDateTime(period.From.ToLocalTime()),
            dateTo: DateOnly.FromDateTime(period.To.ToLocalTime()));
}
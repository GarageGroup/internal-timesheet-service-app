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
            PeriodJson.DataverseSetGetInput(todayProvider.Today), cancellationToken)
        .PipeValue(
            dataverseApi.GetEntitySetAsync<PeriodJson>)
        .Map(
            @out => new PeriodSetGetOut
            {
                Periods = @out.Value.Map(MapPeriod)
            },
            static failure => failure.WithFailureCode<Unit>(default));

    private PeriodItem MapPeriod(PeriodJson period)
        =>
        new(
            name: period.Name,
            dateFrom: DateOnly.FromDateTime(period.From.ToLocalTime()),
            dateTo: DateOnly.FromDateTime(todayProvider.Today < period.To ? todayProvider.Today.ToLocalTime() : period.To.ToLocalTime()));
}
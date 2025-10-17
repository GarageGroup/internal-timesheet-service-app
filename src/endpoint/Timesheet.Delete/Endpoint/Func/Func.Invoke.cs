using GarageGroup.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TagSetGetFunc
{
    public ValueTask<Result<TagSetGetOut, Failure<Unit>>> InvokeAsync(
        TagSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildDbQuery)
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbTag>)
        .MapSuccess(
            static success => new TagSetGetOut
            {
                Tags = success.AsEnumerable().SelectMany(GetHashTags).Distinct().ToFlatArray()
            });

    private DbSelectQuery BuildDbQuery(TagSetGetIn input)
    {
        var maxDate = todayProvider.Today;
        var minDate = maxDate.AddDays(-option.DaysPeriod);

        return DbTag.QueryAll with
        {
            Filter = new DbCombinedFilter(DbLogicalOperator.And)
            {
                Filters =
                [
                    DbTag.BuildOwnerFilter(input.SystemUserId),
                    DbTag.BuildProjectFilter(input.ProjectId),
                    DescriptionTagFilter,
                    DbTag.BuildMinDateFilter(minDate),
                    DbTag.BuildMaxDateFilter(maxDate)
                ]
            },
            Orders = DbTag.DefaultOrders
        };
    }

    private static IEnumerable<string> GetHashTags(DbTag timesheet)
    {
        if (string.IsNullOrWhiteSpace(timesheet.Description))
        {
            yield break;
        }

        foreach (var tagMatch in TagRegex.Matches(timesheet.Description).Cast<Match>())
        {
            yield return tagMatch.Value;
        }
    }
}
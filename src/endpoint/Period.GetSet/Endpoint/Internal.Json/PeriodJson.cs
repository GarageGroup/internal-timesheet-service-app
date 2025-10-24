using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class PeriodJson
{
    private const string EntityPluralName = "gg_employee_cost_periods";

    private const string NameFieldName = "gg_name";

    private const string FromDateFieldName = "gg_from_date";

    private const string ToDateFieldName = "gg_to_date";

    internal static DataverseEntitySetGetIn DataverseSetGetInput(DateTime dateTime)
        =>
        new(
            entityPluralName: EntityPluralName,
            selectFields: [NameFieldName, FromDateFieldName, ToDateFieldName],
            filter: $"statecode eq 0 and {FromDateFieldName} lt {dateTime:O}",
            orderBy:
            [
                new(ToDateFieldName, DataverseOrderDirection.Descending)
            ]);

    [JsonPropertyName(NameFieldName)]
    public string? Name { get; init; }

    [JsonPropertyName(FromDateFieldName)]
    public DateTime From { get; init; }

    [JsonPropertyName(ToDateFieldName)]
    public DateTime To { get; init; }
}
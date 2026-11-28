using System;
using System.Linq;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class PeriodJson
{
    private const string EntityPluralName = "gg_employee_cost_periods";

    private const string NameFieldName = "gg_name";

    private const string FromDateFieldName = "gg_from_date";

    private const string ToDateFieldName = "gg_to_date";

    private const string DateFormat = "yyyy-MM-dd";

    // A closed period is a deactivated one. The input finds a closed period that contains any of the dates
    internal static DataverseEntitySetGetIn BuildClosedPeriodSetGetInput(params DateOnly[] dates)
        =>
        new(
            entityPluralName: EntityPluralName,
            selectFields: [NameFieldName, FromDateFieldName, ToDateFieldName],
            filter: $"statecode eq 1 and ({string.Join(" or ", dates.Select(BuildDateFilter))})",
            top: 1);

    private static string BuildDateFilter(DateOnly date)
        =>
        $"({FromDateFieldName} le {date.ToString(DateFormat)} and {ToDateFieldName} ge {date.ToString(DateFormat)})";

    [JsonPropertyName(NameFieldName)]
    public string? Name { get; init; }

    [JsonPropertyName(FromDateFieldName)]
    public DateOnly From { get; init; }

    [JsonPropertyName(ToDateFieldName)]
    public DateOnly To { get; init; }
}

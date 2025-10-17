using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[DbEntity("gg_timesheetactivity", AliasName)]
internal sealed partial record class DbTag : IDbEntity<DbTag>
{
    private const string All = "QueryAll";

    private const string AliasName = "t";

    private const string DescriptionFieldName = $"{AliasName}.gg_description";

    private const string DateFieldName = $"{AliasName}.gg_date";

    private const string DateFormat = "yyyy-MM-dd";
}
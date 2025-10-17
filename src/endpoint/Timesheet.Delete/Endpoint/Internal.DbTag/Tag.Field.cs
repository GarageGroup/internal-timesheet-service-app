using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTag
{
    [DbSelect(All, AliasName, DescriptionFieldName)]
    public string? Description { get; init; }
}
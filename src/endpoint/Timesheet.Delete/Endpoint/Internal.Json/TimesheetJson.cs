using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    private const string EntityPluralName
        =
        "gg_timesheetactivities";

    internal static DataverseEntityDeleteIn BuildDataverseDeleteInput(Guid timesheetId, Guid callerObjectId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId))
        {
            CallerObjectId = callerObjectId
        };
}
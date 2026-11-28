using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetDeleteDependency
{
    public static Dependency<ITimesheetDeleteFunc> UseTimesheetDeleteFunc(
        this Dependency<IDataverseApiClient> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<ITimesheetDeleteFunc>(CreateFunc);

        static TimesheetDeleteFunc CreateFunc(IDataverseApiClient dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);

            return new(dataverseApi);
        }
    }

    public static Dependency<TimesheetDeleteEndpoint> UseTimesheetDeleteEndpoint(
        this Dependency<IDataverseApiClient> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UseTimesheetDeleteFunc().Map(TimesheetDeleteEndpoint.Resolve);
    }
}

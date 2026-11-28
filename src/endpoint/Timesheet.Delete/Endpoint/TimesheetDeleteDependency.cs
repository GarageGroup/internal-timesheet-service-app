using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetDeleteDependency
{
    public static Dependency<ITimesheetDeleteFunc> UseTimesheetDeleteFunc(
        this Dependency<IDataverseApiClient, IOpenPeriodApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<ITimesheetDeleteFunc>(CreateFunc);

        static TimesheetDeleteFunc CreateFunc(IDataverseApiClient dataverseApi, IOpenPeriodApi openPeriodApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(openPeriodApi);

            return new(dataverseApi, openPeriodApi);
        }
    }

    public static Dependency<TimesheetDeleteEndpoint> UseTimesheetDeleteEndpoint(
        this Dependency<IDataverseApiClient, IOpenPeriodApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UseTimesheetDeleteFunc().Map(TimesheetDeleteEndpoint.Resolve);
    }
}

using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetModifyDependency
{
    public static Dependency<ITimesheetUpdateFunc> UseTimesheetUpdateFunc(
        this Dependency<IDataverseApiClient, IOpenPeriodApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<ITimesheetUpdateFunc>(CreateApi);

        static TimesheetModifyApi CreateApi(IDataverseApiClient dataverseApi, IOpenPeriodApi openPeriodApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(openPeriodApi);

            return new(dataverseApi, openPeriodApi, TodayProvider.Instance);
        }
    }

    public static Dependency<ITimesheetCreateFunc> UseTimesheetCreateFunc(
        this Dependency<IDataverseApiClient, IOpenPeriodApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<ITimesheetCreateFunc>(CreateApi);

        static TimesheetModifyApi CreateApi(IDataverseApiClient dataverseApi, IOpenPeriodApi openPeriodApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(openPeriodApi);

            return new(dataverseApi, openPeriodApi, TodayProvider.Instance);
        }
    }

    public static Dependency<TimesheetModifyEndpointSet> UseTimesheetModifyEndpointSet(
        this Dependency<IDataverseApiClient, IOpenPeriodApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold(CreateApi).Map(TimesheetModifyEndpointSet.Resolve);

        static TimesheetModifyApi CreateApi(IDataverseApiClient dataverseApi, IOpenPeriodApi openPeriodApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(openPeriodApi);
            return new(dataverseApi, openPeriodApi, TodayProvider.Instance);
        }
    }
}

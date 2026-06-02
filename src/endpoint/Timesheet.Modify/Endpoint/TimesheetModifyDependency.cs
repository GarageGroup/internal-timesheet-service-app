using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetModifyDependency
{
    public static Dependency<TimesheetModifyEndpointSet> UseTimesheetModifyEndpointSet(
        this Dependency<IDataverseApiClient> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map(CreateApi).Map(TimesheetModifyEndpointSet.Resolve);

        static TimesheetModifyApi CreateApi(IDataverseApiClient dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new(dataverseApi);
        }
    }
}
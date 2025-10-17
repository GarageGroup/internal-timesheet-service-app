using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetDeleteDependency
{
    public static Dependency<TimesheetDeleteEndpoint> UseTimesheetDeleteEndpoint<TDataverseApi>(
        this Dependency<TDataverseApi> dependency)
        where TDataverseApi : IDataverseEntityDeleteSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map(CreateFunc).Map(TimesheetDeleteEndpoint.Resolve);

        static TimesheetDeleteFunc CreateFunc(TDataverseApi dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new(dataverseApi);
        }
    }
}
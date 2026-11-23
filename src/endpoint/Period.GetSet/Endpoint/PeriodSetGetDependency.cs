using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Period.SetGet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class PeriodSetGetDependency
{
    public static Dependency<IPeriodSetGetFunc> UsePeriodSetGetFunc<TDataverseApi>(
        this Dependency<TDataverseApi> dependency)
        where TDataverseApi : IDataverseEntitySetGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IPeriodSetGetFunc>(CreateFunc);

        static PeriodSetGetFunc CreateFunc(TDataverseApi dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);

            return new(dataverseApi, TodayProvider.Instance);
        }
    }

    public static Dependency<PeriodSetGetEndpoint> UsePeriodSetGetEndpoint<TDataverseApi>(
        this Dependency<TDataverseApi> dependency)
        where TDataverseApi : IDataverseEntitySetGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UsePeriodSetGetFunc().Map(PeriodSetGetEndpoint.Resolve);
    }
}

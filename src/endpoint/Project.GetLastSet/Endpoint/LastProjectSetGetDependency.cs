using GarageGroup.Infra;
using PrimeFuncPack;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Project.LastSetGet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class LastProjectSetGetDependency
{
    public static Dependency<ILastProjectSetGetFunc> UseLastProjectSetGetFunc<TSqlApi>(
        this Dependency<TSqlApi, LastProjectSetGetOption> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<ILastProjectSetGetFunc>(CreateFunc);

        static LastProjectSetGetFunc CreateFunc(TSqlApi sqlApi, LastProjectSetGetOption option)
        {
            ArgumentNullException.ThrowIfNull(sqlApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(sqlApi, TodayProvider.Instance, option);
        }
    }

    public static Dependency<LastProjectSetGetEndpoint> UseLastProjectSetGetEndpoint<TSqlApi>(
        this Dependency<TSqlApi, LastProjectSetGetOption> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UseLastProjectSetGetFunc().Map(LastProjectSetGetEndpoint.Resolve);
    }
}

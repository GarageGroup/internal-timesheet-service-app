using GarageGroup.Infra;
using PrimeFuncPack;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Project.SetGet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class ProjectSetGetDependency
{
    public static Dependency<IProjectSetGetFunc> UseProjectSetGetFunc<TSqlApi>(
        this Dependency<TSqlApi> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IProjectSetGetFunc>(CreateFunc);

        static ProjectSetGetFunc CreateFunc(TSqlApi sqlApi)
        {
            ArgumentNullException.ThrowIfNull(sqlApi);

            return new(sqlApi, TodayProvider.Instance);
        }
    }

    public static Dependency<ProjectSetGetEndpoint> UseProjectSetGetEndpoint<TSqlApi>(
        this Dependency<TSqlApi> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UseProjectSetGetFunc().Map(ProjectSetGetEndpoint.Resolve);
    }
}

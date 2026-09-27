using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Project.SearchSet.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class ProjectSetSearchDependency
{
    public static Dependency<IProjectSetSearchFunc> UseProjectSetSearchFunc<TDataverseApi>(
        this Dependency<TDataverseApi> dependency)
        where TDataverseApi : IDataverseSearchSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map<IProjectSetSearchFunc>(CreateFunc);

        static ProjectSetSearchFunc CreateFunc(TDataverseApi dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new(dataverseApi);
        }
    }

    public static Dependency<ProjectSetSearchEndpoint> UseProjectSetSearchEndpoint<TDataverseApi>(
        this Dependency<TDataverseApi> dependency)
        where TDataverseApi : IDataverseSearchSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.UseProjectSetSearchFunc().Map(ProjectSetSearchEndpoint.Resolve);
    }
}

using GarageGroup.Infra;
using PrimeFuncPack;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Tag.GetSet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class TagSetGetFuncDependency
{
    public static Dependency<TagSetGetEndpoint> UseTagGetSetEndpoint<TSqlApi>(
        this Dependency<TSqlApi, TagSetGetOption> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold(CreateFunc).Map(TagSetGetEndpoint.Resolve);

        static TagSetGetFunc CreateFunc(TSqlApi sqlApi, TagSetGetOption option)
        {
            ArgumentNullException.ThrowIfNull(sqlApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(sqlApi, TodayProvider.Instance, option);
        }
    }
}
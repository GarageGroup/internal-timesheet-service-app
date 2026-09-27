using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Identity.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentUserContextResolverDependency
{
    public static Dependency<IAgentUserContextResolver> UseAgentUserContextResolver<TSqlApi>(this Dependency<TSqlApi> dependency)
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IAgentUserContextResolver>(CreateResolver);

        static AgentUserContextResolver CreateResolver(TSqlApi sqlApi)
        {
            ArgumentNullException.ThrowIfNull(sqlApi);

            return new(sqlApi);
        }
    }
}

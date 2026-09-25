using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Agent.Profile.Get.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentProfileGetDependency
{
    public static Dependency<AgentProfileGetEndpoint> UseAgentProfileGetEndpoint(
        this Dependency<IAgentUserContextResolver, IProfileGetFunc> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentProfileGetFunc>(CreateFunc).Map(AgentProfileGetEndpoint.Resolve);

        static AgentProfileGetFunc CreateFunc(IAgentUserContextResolver resolver, IProfileGetFunc profileGetFunc)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(profileGetFunc);
            return new(resolver, profileGetFunc);
        }
    }
}

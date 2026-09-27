using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentDependency
{
    public static Dependency<IAgentProjectSetSearchFunc> UseAgentProjectSetSearchFunc(
        this Dependency<IProjectSetSearchFunc, AgentProjectSetSearchOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IAgentProjectSetSearchFunc>(CreateFunc);

        static AgentProjectSetSearchFunc CreateFunc(IProjectSetSearchFunc projectSetSearchFunc, AgentProjectSetSearchOption option)
        {
            ArgumentNullException.ThrowIfNull(projectSetSearchFunc);
            ArgumentNullException.ThrowIfNull(option);
            return new(projectSetSearchFunc, option);
        }
    }

    public static Dependency<IAgentTimesheetSetGetFunc> UseAgentTimesheetSetGetFunc(
        this Dependency<ITimesheetSetGetFunc, AgentTimesheetSetGetOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IAgentTimesheetSetGetFunc>(CreateFunc);

        static AgentTimesheetSetGetFunc CreateFunc(ITimesheetSetGetFunc timesheetSetGetFunc, AgentTimesheetSetGetOption option)
        {
            ArgumentNullException.ThrowIfNull(timesheetSetGetFunc);
            ArgumentNullException.ThrowIfNull(option);
            return new(timesheetSetGetFunc, option);
        }
    }
}

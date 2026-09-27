using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentDependency
{
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

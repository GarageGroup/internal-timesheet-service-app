using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Agent.Action.Decide.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentActionDecideDependency
{
    public static Dependency<AgentActionDecideEndpoint> UseAgentActionDecideEndpoint(
        this Dependency<
            IAgentUserContextResolver,
            IAgentTimesheetCreateConfirmFunc,
            IAgentTimesheetCreateCancelFunc,
            IAgentTimesheetDeleteConfirmFunc,
            IAgentTimesheetDeleteCancelFunc,
            IAgentTimesheetUpdateConfirmFunc,
            IAgentTimesheetUpdateCancelFunc,
            AgentActionDecideContext> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentActionDecideFunc>(CreateFunc).Map(AgentActionDecideEndpoint.Resolve);

        static AgentActionDecideFunc CreateFunc(
            IAgentUserContextResolver resolver,
            IAgentTimesheetCreateConfirmFunc createConfirmFunc,
            IAgentTimesheetCreateCancelFunc createCancelFunc,
            IAgentTimesheetDeleteConfirmFunc deleteConfirmFunc,
            IAgentTimesheetDeleteCancelFunc deleteCancelFunc,
            IAgentTimesheetUpdateConfirmFunc updateConfirmFunc,
            IAgentTimesheetUpdateCancelFunc updateCancelFunc,
            AgentActionDecideContext context)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(createConfirmFunc);
            ArgumentNullException.ThrowIfNull(createCancelFunc);
            ArgumentNullException.ThrowIfNull(deleteConfirmFunc);
            ArgumentNullException.ThrowIfNull(deleteCancelFunc);
            ArgumentNullException.ThrowIfNull(updateConfirmFunc);
            ArgumentNullException.ThrowIfNull(updateCancelFunc);
            ArgumentNullException.ThrowIfNull(context);

            return new(
                resolver,
                createConfirmFunc,
                createCancelFunc,
                deleteConfirmFunc,
                deleteCancelFunc,
                updateConfirmFunc,
                updateCancelFunc,
                context.TimesheetSetGetFunc,
                context.Option);
        }
    }
}

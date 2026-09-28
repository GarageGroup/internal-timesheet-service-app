using System;
using System.Runtime.CompilerServices;
using Azure.Core;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentDependency
{
    public static Dependency<IAgentConversationMessageFunc> UseAgentConversationMessageFunc(
        this Dependency<IAgentMessageFunc, IAgentConversationStore> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentConversationMessageFunc>(CreateFunc);

        static AgentConversationMessageFunc CreateFunc(
            IAgentMessageFunc messageFunc,
            IAgentConversationStore conversationStore)
        {
            ArgumentNullException.ThrowIfNull(messageFunc);
            ArgumentNullException.ThrowIfNull(conversationStore);

            return new(messageFunc, conversationStore);
        }
    }

    public static Dependency<IAgentMessageFunc> UseAgentMessageFunc(
        this Dependency<IAgentKernelFactory, AgentMessageOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentMessageFunc>(CreateFunc);

        static AgentMessageFunc CreateFunc(
            IAgentKernelFactory kernelFactory,
            AgentMessageOption option)
        {
            ArgumentNullException.ThrowIfNull(kernelFactory);
            ArgumentNullException.ThrowIfNull(option);

            return new(kernelFactory, new DateProvider(option.TimeZone), option);
        }
    }

    public static Dependency<IAgentKernelFactory> UseAgentKernelFactory(
        this Dependency<
            IAgentTimesheetSetGetFunc,
            IAgentProjectSetSearchFunc,
            IAgentLastProjectSetGetFunc,
            IAgentPeriodSetGetFunc,
            IAgentTagSetGetFunc,
            TokenCredential,
            AgentFoundryOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentKernelFactory>(CreateFactory);

        static AgentKernelFactory CreateFactory(
            IAgentTimesheetSetGetFunc timesheetSetGetFunc,
            IAgentProjectSetSearchFunc projectSetSearchFunc,
            IAgentLastProjectSetGetFunc lastProjectSetGetFunc,
            IAgentPeriodSetGetFunc periodSetGetFunc,
            IAgentTagSetGetFunc tagSetGetFunc,
            TokenCredential tokenCredential,
            AgentFoundryOption option)
        {
            ArgumentNullException.ThrowIfNull(timesheetSetGetFunc);
            ArgumentNullException.ThrowIfNull(projectSetSearchFunc);
            ArgumentNullException.ThrowIfNull(lastProjectSetGetFunc);
            ArgumentNullException.ThrowIfNull(periodSetGetFunc);
            ArgumentNullException.ThrowIfNull(tagSetGetFunc);
            ArgumentNullException.ThrowIfNull(tokenCredential);
            ArgumentNullException.ThrowIfNull(option);

            return new(
                timesheetSetGetFunc,
                projectSetSearchFunc,
                lastProjectSetGetFunc,
                periodSetGetFunc,
                tagSetGetFunc,
                tokenCredential,
                option);
        }
    }

    public static Dependency<IAgentTagSetGetFunc> UseAgentTagSetGetFunc(
        this Dependency<ITagSetGetFunc, AgentTagSetGetOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentTagSetGetFunc>(CreateFunc);

        static AgentTagSetGetFunc CreateFunc(ITagSetGetFunc tagSetGetFunc, AgentTagSetGetOption option)
        {
            ArgumentNullException.ThrowIfNull(tagSetGetFunc);
            ArgumentNullException.ThrowIfNull(option);

            return new(tagSetGetFunc, option);
        }
    }

    public static Dependency<IAgentPeriodSetGetFunc> UseAgentPeriodSetGetFunc(
        this Dependency<IPeriodSetGetFunc> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IAgentPeriodSetGetFunc>(CreateFunc);

        static AgentPeriodSetGetFunc CreateFunc(IPeriodSetGetFunc periodSetGetFunc)
        {
            ArgumentNullException.ThrowIfNull(periodSetGetFunc);

            return new(periodSetGetFunc);
        }
    }

    public static Dependency<IAgentLastProjectSetGetFunc> UseAgentLastProjectSetGetFunc(
        this Dependency<ILastProjectSetGetFunc, AgentLastProjectSetGetOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentLastProjectSetGetFunc>(CreateFunc);

        static AgentLastProjectSetGetFunc CreateFunc(ILastProjectSetGetFunc lastProjectSetGetFunc, AgentLastProjectSetGetOption option)
        {
            ArgumentNullException.ThrowIfNull(lastProjectSetGetFunc);
            ArgumentNullException.ThrowIfNull(option);

            return new(lastProjectSetGetFunc, option);
        }
    }

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

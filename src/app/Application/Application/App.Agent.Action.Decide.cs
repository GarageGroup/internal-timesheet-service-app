using System;
using GarageGroup.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<AgentActionDecideEndpoint> UseAgentActionDecideEndpoint()
        =>
        Pipeline.Pipe(
            UseSqlApi())
        .UseAgentUserContextResolver()
        .With(
            UseAgentTimesheetCreateConfirmFunc())
        .With(
            UseAgentTimesheetCreateCancelFunc())
        .With(
            ResolveAgentActionDecideOption)
        .UseAgentActionDecideEndpoint();

    private static Dependency<IAgentTimesheetCreateConfirmFunc> UseAgentTimesheetCreateConfirmFunc()
        =>
        Pipeline.Pipe(
            UseAgentActionStore())
        .With(
            UseDataverseApi().UseTimesheetCreateFunc())
        .UseAgentTimesheetCreateConfirmFunc();

    private static Dependency<IAgentTimesheetCreateCancelFunc> UseAgentTimesheetCreateCancelFunc()
        =>
        UseAgentActionStore().UseAgentTimesheetCreateCancelFunc();

    private static AgentActionDecideOption ResolveAgentActionDecideOption(IServiceProvider serviceProvider)
        =>
        new(serviceProvider.GetConfiguration().GetValue<bool>("Agent:WritePreparation:Enabled"));
}

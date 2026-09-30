using System;
using GarageGroup.Infra;
using Microsoft.Extensions.Configuration;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static Dependency<IAgentTimesheetCreatePrepareFunc> UseAgentTimesheetCreatePrepareFunc()
        =>
        Pipeline.Pipe(
            UseSqlApi().UseProjectSetGetFunc())
        .With(
            UseAgentActionStore())
        .With(
            ResolveAgentTimesheetCreatePrepareOption)
        .UseAgentTimesheetCreatePrepareFunc();

    private static Dependency<IAgentTimesheetDeletePrepareFunc> UseAgentTimesheetDeletePrepareFunc()
        =>
        Pipeline.Pipe(
            UseAgentTimesheetSetGetFunc())
        .With(
            UseAgentTimesheetDeleteActionStore())
        .With(
            ResolveAgentTimesheetDeletePrepareOption)
        .UseAgentTimesheetDeletePrepareFunc();

    private static Dependency<IAgentTimesheetSetGetFunc> UseAgentTimesheetSetGetFunc()
        =>
        Pipeline.Pipe(
            UseSqlApi())
        .UseTimesheetSetGetFunc()
        .With(
            ResolveAgentTimesheetSetGetOption)
        .UseAgentTimesheetSetGetFunc();

    private static Dependency<IAgentProjectSetSearchFunc> UseAgentProjectSetSearchFunc()
        =>
        Pipeline.Pipe(
            UseDataverseApi())
        .UseProjectSetSearchFunc()
        .With(
            ResolveAgentProjectSetSearchOption)
        .UseAgentProjectSetSearchFunc();

    private static Dependency<IAgentLastProjectSetGetFunc> UseAgentLastProjectSetGetFunc()
        =>
        Pipeline.Pipe(
            UseSqlApi())
        .With(
            ResolveLastProjectSetGetOption)
        .UseLastProjectSetGetFunc()
        .With(
            ResolveAgentLastProjectSetGetOption)
        .UseAgentLastProjectSetGetFunc();

    private static Dependency<IAgentPeriodSetGetFunc> UseAgentPeriodSetGetFunc()
        =>
        UseDataverseApi().UsePeriodSetGetFunc().UseAgentPeriodSetGetFunc();

    private static Dependency<IAgentTagSetGetFunc> UseAgentTagSetGetFunc()
        =>
        Pipeline.Pipe(
            UseSqlApi())
        .With(
            ResolveTagSetGetOption)
        .UseTagSetGetFunc()
        .With(
            ResolveAgentTagSetGetOption)
        .UseAgentTagSetGetFunc();

    private static AgentTimesheetSetGetOption ResolveAgentTimesheetSetGetOption(IServiceProvider serviceProvider)
        =>
        new()
        {
            MaxDateRangeInDays = serviceProvider.GetConfiguration().GetValue("Agent:Tools:Timesheet:MaxDateRangeInDays", 31)
        };

    private static AgentProjectSetSearchOption ResolveAgentProjectSetSearchOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();

        return new()
        {
            DefaultTop = configuration.GetValue("Agent:Tools:Project:DefaultTop", 10),
            MaxTop = configuration.GetValue("Agent:Tools:Project:MaxTop", 20),
            MaxSearchTextLength = configuration.GetValue("Agent:Tools:Project:MaxSearchTextLength", 100)
        };
    }

    private static AgentLastProjectSetGetOption ResolveAgentLastProjectSetGetOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();

        return new()
        {
            DefaultTop = configuration.GetValue("Agent:Tools:Project:DefaultTop", 10),
            MaxTop = configuration.GetValue("Agent:Tools:Project:MaxTop", 20)
        };
    }

    private static AgentTagSetGetOption ResolveAgentTagSetGetOption(IServiceProvider serviceProvider)
        =>
        new()
        {
            MaxTags = serviceProvider.GetConfiguration().GetValue("Agent:Tools:Tag:MaxTags", 20)
        };

    private static AgentTimesheetCreatePrepareOption ResolveAgentTimesheetCreatePrepareOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();
        var approvalTtlMinutes = configuration.GetValue("Agent:WritePreparation:ApprovalTtlMinutes", 10);

        if (approvalTtlMinutes <= 0)
        {
            throw new InvalidOperationException("Agent action approval TTL must be positive");
        }

        return new()
        {
            ApprovalTtl = TimeSpan.FromMinutes(approvalTtlMinutes)
        };
    }

    private static AgentTimesheetDeletePrepareOption ResolveAgentTimesheetDeletePrepareOption(IServiceProvider serviceProvider)
        =>
        new()
        {
            ApprovalTtl = ResolveAgentTimesheetCreatePrepareOption(serviceProvider).ApprovalTtl
        };
}

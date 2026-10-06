using System;
using System.Linq;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static Dependency<IAgentAudioTranscribeFunc> UseAgentAudioTranscribeFunc()
        =>
        Dependency.From(
            ResolveTokenCredential)
        .With(
            ResolveAgentAudioProviderOption)
        .UseAzureOpenAIAudioToTextService()
        .With(
            ResolveAgentAudioTranscribeOption)
        .UseAgentAudioTranscribeFunc();

    private static Dependency<IAgentConversationMessageFunc> UseAgentConversationMessageFunc()
        =>
        Pipeline.Pipe(
            UseAgentMessageFunc())
        .With(
            UseAgentConversationStore())
        .With(
            ResolveAgentConversationMessageOption)
        .UseAgentConversationMessageFunc();

    private static Dependency<IAgentConversationStore> UseAgentConversationStore()
        =>
        Dependency.From(
            ResolveTokenCredential)
        .With(
            ResolveAgentConversationTableOption)
        .UseAgentConversationTableStore();

    private static Dependency<IAgentActionStore> UseAgentActionStore()
        =>
        Dependency.From(
            ResolveTokenCredential)
        .With(
            ResolveAgentActionTableOption)
        .UseAgentActionTableStore();

    private static Dependency<IAgentTimesheetDeleteActionStore> UseAgentTimesheetDeleteActionStore()
        =>
        Dependency.From(
            ResolveTokenCredential)
        .With(
            ResolveAgentActionTableOption)
        .UseAgentTimesheetDeleteActionTableStore();

    private static Dependency<IAgentTimesheetUpdateActionStore> UseAgentTimesheetUpdateActionStore()
        =>
        Dependency.From(
            ResolveTokenCredential)
        .With(
            ResolveAgentActionTableOption)
        .UseAgentTimesheetUpdateActionTableStore();

    private static Dependency<IAgentMessageFunc> UseAgentMessageFunc()
        =>
        Pipeline.Pipe(
            UseAgentKernelFactory())
        .With(
            ResolveAgentMessageOption)
        .UseAgentMessageFunc();

    private static Dependency<IAgentKernelFactory> UseAgentKernelFactory()
        =>
        Pipeline.Pipe(
            UseAgentKernelToolSet())
        .With(
            ResolveTokenCredential)
        .With(
            ResolveAgentFoundryOption)
        .With(
            ResolveAgentWritePreparationOption)
        .UseAgentKernelFactory();

    private static Dependency<AgentKernelToolSet> UseAgentKernelToolSet()
        =>
        Pipeline.Pipe(
            UseAgentTimesheetSetGetFunc())
        .With(
            UseAgentProjectSetSearchFunc())
        .With(
            UseAgentLastProjectSetGetFunc())
        .With(
            UseAgentPeriodSetGetFunc())
        .With(
            UseAgentTagSetGetFunc())
        .With(
            UseAgentTimesheetCreatePrepareFunc())
        .With(
            UseAgentTimesheetDeletePrepareFunc())
        .With(
            UseAgentTimesheetUpdatePrepareFunc())
        .UseAgentKernelToolSet();

    private static TokenCredential ResolveTokenCredential(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<TokenCredential>();

    private static AgentMessageOption ResolveAgentMessageOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();
        var timeZoneId = configuration["Agent:Message:TimeZoneId"];
        var maxTextLength = configuration.GetValue("Agent:Message:MaxTextLength", 2000);
        var maxHistoryMessageCount = configuration.GetValue("Agent:Message:MaxHistoryMessageCount", 20);

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("Agent message time zone ID must be specified");
        }

        if (maxTextLength <= 0)
        {
            throw new InvalidOperationException("Agent message maximum text length must be positive");
        }

        if (maxHistoryMessageCount < 2)
        {
            throw new InvalidOperationException("Agent message maximum history message count must be at least two");
        }

        return new(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId), maxTextLength)
        {
            MaxHistoryMessageCount = maxHistoryMessageCount,
            WritePreparationEnabled = configuration.GetValue<bool>("Agent:WritePreparation:Enabled")
        };
    }

    private static AgentConversationMessageOption ResolveAgentConversationMessageOption(IServiceProvider serviceProvider)
        =>
        new(serviceProvider.GetConfiguration().GetValue("Agent:Message:MaxHistoryMessageCount", 20));

    private static AgentConversationTableOption ResolveAgentConversationTableOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();

        return new(
            ResolveAgentTableServiceEndpoint(configuration),
            ResolveAgentTableName(configuration, "ConversationTableName", "conversation"));
    }

    private static AgentActionTableOption ResolveAgentActionTableOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();

        return new(
            ResolveAgentTableServiceEndpoint(configuration),
            ResolveAgentTableName(configuration, "ActionTableName", "action"));
    }

    private static Uri ResolveAgentTableServiceEndpoint(IConfiguration configuration)
    {
        var endpointValue = configuration["Agent:Storage:TableServiceEndpoint"];

        if (Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) is false ||
            endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) is false)
        {
            throw new InvalidOperationException("Agent storage table service endpoint must be an absolute HTTPS URL");
        }

        return endpoint;
    }

    private static string ResolveAgentTableName(IConfiguration configuration, string key, string description)
    {
        var tableName = configuration[$"Agent:Storage:{key}"];
        if (string.IsNullOrWhiteSpace(tableName) ||
            tableName.Length is < 3 or > 63 ||
            char.IsLetter(tableName[0]) is false ||
            tableName.Any(static c => char.IsLetterOrDigit(c) is false))
        {
            throw new InvalidOperationException($"Agent storage {description} table name must be specified");
        }

        return tableName.Trim();
    }

    private static AgentWritePreparationOption ResolveAgentWritePreparationOption(IServiceProvider serviceProvider)
        =>
        new(serviceProvider.GetConfiguration().GetValue<bool>("Agent:WritePreparation:Enabled"));

    private static AgentFoundryOption ResolveAgentFoundryOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();
        var endpointValue = configuration["Agent:Foundry:ProjectEndpoint"];
        var modelId = configuration["Agent:Foundry:ModelId"];
        var tokenScope = configuration["Agent:Foundry:TokenScope"];

        if (Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) is false ||
            endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) is false)
        {
            throw new InvalidOperationException("Agent Foundry project endpoint must be an absolute HTTPS URL");
        }

        if (endpoint.Host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase) is false ||
            endpoint.AbsolutePath.Contains("/api/projects/", StringComparison.OrdinalIgnoreCase) is false)
        {
            throw new InvalidOperationException("Agent Foundry project endpoint has an unsupported format");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent Foundry model ID must be specified");
        }

        if (string.IsNullOrWhiteSpace(tokenScope))
        {
            throw new InvalidOperationException("Agent Foundry token scope must be specified");
        }

        return new(endpoint, modelId.Trim(), tokenScope.Trim());
    }

    private static AgentAudioProviderOption ResolveAgentAudioProviderOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();
        if (configuration.GetValue<bool>("Agent:Voice:Enabled") is false)
        {
            return new(new("https://localhost"), "disabled", "disabled");
        }

        var endpointValue = configuration["Agent:Voice:Endpoint"];
        var deploymentName = configuration["Agent:Voice:DeploymentName"];
        var modelId = configuration["Agent:Voice:ModelId"];

        if (Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) is false ||
            endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) is false)
        {
            throw new InvalidOperationException("Agent audio endpoint must be an absolute HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new InvalidOperationException("Agent audio deployment name must be specified");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent audio model ID must be specified");
        }

        return new(endpoint, deploymentName.Trim(), modelId.Trim());
    }

    private static AgentAudioTranscribeOption ResolveAgentAudioTranscribeOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetConfiguration();
        var maxFileSizeBytes = configuration.GetValue("Agent:Voice:MaxFileSizeBytes", 5 * 1024 * 1024);
        if (maxFileSizeBytes <= 0)
        {
            throw new InvalidOperationException("Agent audio maximum file size must be positive");
        }

        return AgentAudioTranscribeOption.Default with
        {
            Enabled = configuration.GetValue<bool>("Agent:Voice:Enabled"),
            MaxFileSizeBytes = maxFileSizeBytes
        };
    }
}

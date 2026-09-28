using System;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static Dependency<AgentKernelFactory> UseAgentKernelFactory()
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
            ResolveTokenCredential)
        .With(
            ResolveAgentFoundryOption)
        .UseAgentKernelFactory();

    private static TokenCredential ResolveTokenCredential(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<TokenCredential>();

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
}

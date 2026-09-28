using System;
using System.ClientModel.Primitives;
using Azure.Core;
using Microsoft.SemanticKernel;
using OpenAI;

namespace GarageGroup.Internal.Timesheet;

public sealed class AgentKernelFactory(
    IAgentTimesheetSetGetFunc timesheetSetGetFunc,
    IAgentProjectSetSearchFunc projectSetSearchFunc,
    IAgentLastProjectSetGetFunc lastProjectSetGetFunc,
    IAgentPeriodSetGetFunc periodSetGetFunc,
    IAgentTagSetGetFunc tagSetGetFunc,
    TokenCredential tokenCredential,
    AgentFoundryOption option)
{
    public Kernel Create(AgentUserContext context)
    {
        var builder = Kernel.CreateBuilder();
        var tokenPolicy = new BearerTokenPolicy(tokenCredential, option.TokenScope);
        var endpoint = new Uri($"{option.ProjectEndpoint.AbsoluteUri.TrimEnd('/')}/openai/v1/");

#pragma warning disable OPENAI001
        OpenAIClient openAiClient = new(
            authenticationPolicy: tokenPolicy,
            options: new()
            {
                Endpoint = endpoint
            });
#pragma warning restore OPENAI001

        builder.AddOpenAIChatCompletion(option.ModelId, openAiClient);

        var kernel = builder.Build();
        kernel.Plugins.AddFromObject(
            new AgentReadPlugin(
                context,
                timesheetSetGetFunc,
                projectSetSearchFunc,
                lastProjectSetGetFunc,
                periodSetGetFunc,
                tagSetGetFunc),
            AgentReadPlugin.PluginName);

        return kernel;
    }
}

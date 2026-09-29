using System;
using System.ClientModel.Primitives;
using Azure.Core;
using Microsoft.SemanticKernel;
using OpenAI;

namespace GarageGroup.Internal.Timesheet;

public sealed class AgentKernelFactory(
    AgentKernelToolSet toolSet,
    TokenCredential tokenCredential,
    AgentFoundryOption option,
    AgentWritePreparationOption writePreparationOption) : IAgentKernelFactory
{
    public AgentKernelScope Create(AgentUserContext context)
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
        var actionCapture = new AgentPreparedActionCapture();
        kernel.Plugins.AddFromObject(
            new AgentReadPlugin(
                context,
                toolSet.TimesheetSetGetFunc,
                toolSet.ProjectSetSearchFunc,
                toolSet.LastProjectSetGetFunc,
                toolSet.PeriodSetGetFunc,
                toolSet.TagSetGetFunc),
            AgentReadPlugin.PluginName);

        if (writePreparationOption.Enabled)
        {
            kernel.Plugins.AddFromObject(
                new AgentWritePlugin(context, toolSet.TimesheetCreatePrepareFunc, actionCapture),
                AgentWritePlugin.PluginName);
        }

        return new(kernel, actionCapture);
    }
}

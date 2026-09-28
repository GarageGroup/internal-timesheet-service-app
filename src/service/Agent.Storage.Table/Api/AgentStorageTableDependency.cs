using System;
using System.Runtime.CompilerServices;
using Azure.Core;
using Azure.Data.Tables;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentStorageTableDependency
{
    public static Dependency<IAgentRequestStore> UseAgentRequestTableStore(
        this Dependency<TokenCredential, AgentRequestTableOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentRequestStore>(CreateStore);

        static AgentRequestTableApi CreateStore(TokenCredential credential, AgentRequestTableOption option)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(option);

            return new(new TableApi(new TableClient(option.ServiceEndpoint, option.TableName, credential)));
        }
    }

    public static Dependency<IAgentConversationStore> UseAgentConversationTableStore(
        this Dependency<TokenCredential, AgentConversationTableOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentConversationStore>(CreateStore);

        static AgentConversationTableApi CreateStore(TokenCredential credential, AgentConversationTableOption option)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(option);

            return new(new TableApi(new TableClient(option.ServiceEndpoint, option.TableName, credential)));
        }
    }
}

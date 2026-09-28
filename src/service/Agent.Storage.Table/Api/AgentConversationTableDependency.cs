using System;
using Azure.Core;
using Azure.Data.Tables;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class AgentConversationTableDependency
{
    public static Dependency<IAgentConversationStore> UseAgentConversationTableStore(
        this Dependency<TokenCredential, AgentConversationTableOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentConversationStore>(CreateStore);

        static AgentConversationTableStore CreateStore(TokenCredential credential, AgentConversationTableOption option)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(option);

            return new(new TableClient(option.ServiceEndpoint, option.TableName, credential));
        }
    }
}

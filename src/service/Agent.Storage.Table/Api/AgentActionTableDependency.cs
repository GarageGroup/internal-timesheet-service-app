using System;
using Azure.Core;
using Azure.Data.Tables;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class AgentActionTableDependency
{
    public static Dependency<IAgentActionStore> UseAgentActionTableStore(
        this Dependency<TokenCredential, AgentActionTableOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentActionStore>(CreateStore);

        static AgentActionTableApi CreateStore(TokenCredential credential, AgentActionTableOption option)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(option);

            return new(new TableApi(new TableClient(option.ServiceEndpoint, option.TableName, credential)));
        }
    }
}

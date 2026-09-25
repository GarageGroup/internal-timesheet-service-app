using GarageGroup.Infra;
using PrimeFuncPack;
using System;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<AgentProfileGetEndpoint> UseAgentProfileGetEndpoint()
        =>
        Pipeline.Pipe(UseSqlApi())
        .UseAgentUserContextResolver()
        .With(
            Pipeline.Pipe(UseSqlApi())
            .With(UseBotApi())
            .UseProfileGetFunc())
        .UseAgentProfileGetEndpoint();
}

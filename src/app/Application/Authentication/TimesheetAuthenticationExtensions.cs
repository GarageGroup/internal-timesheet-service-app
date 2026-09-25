using GarageGroup.Infra;
using Microsoft.AspNetCore.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetAuthenticationExtensions
{
    internal static EndpointApplication UseAgentAuthentication(this EndpointApplication app)
    {
        app.Use(AgentAccessMiddleware.InvokeAsync);
        return app;
    }

    internal static EndpointApplication UseLegacyJwtAuthentication(this EndpointApplication app)
    {
        ((IApplicationBuilder)app).UseWhen(
            static context => context.Request.Path.StartsWithSegments("/internal/agent") is false,
            static branch => branch.UseJwtReader());

        return app;
    }
}

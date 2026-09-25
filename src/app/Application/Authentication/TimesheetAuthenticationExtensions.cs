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
}

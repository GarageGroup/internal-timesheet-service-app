using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentAccessMiddleware
{
    private const string BotIdClaimType = "timesheet_bot_id";

    internal static async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/internal/agent") is false)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        if (configuration.GetValue<bool>("Agent:Enabled") is false)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var authentication = await context.AuthenticateAsync(ApplicationHost.AgentAuthenticationScheme).ConfigureAwait(false);

        if (authentication.Succeeded is false || authentication.Principal is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var principal = authentication.Principal;
        var requiredRole = configuration["Agent:Authentication:RequiredRole"];

        if (principal.Claims.Any(c => IsRoleClaim(c) && string.Equals(c.Value, requiredRole, StringComparison.Ordinal)) is false)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var clientId = principal.FindFirstValue("azp") ?? principal.FindFirstValue("appid");
        var botIdText = string.IsNullOrWhiteSpace(clientId)
            ? null
            : configuration[$"Agent:Authentication:Clients:{clientId}:BotId"];

        if (long.TryParse(botIdText, out var botId) is false || botId <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var identity = new ClaimsIdentity(principal.Identity);
        identity.AddClaim(new(BotIdClaimType, botId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        context.User = new ClaimsPrincipal(identity);

        await next(context).ConfigureAwait(false);
    }

    private static bool IsRoleClaim(Claim claim)
        =>
        string.Equals(claim.Type, "roles", StringComparison.Ordinal) ||
        string.Equals(claim.Type, ClaimTypes.Role, StringComparison.Ordinal);
}

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AgentAccessMiddleware));

        if (configuration.GetValue<bool>("Agent:Enabled") is false)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var authentication = await context.AuthenticateAsync(ApplicationHost.AgentAuthenticationScheme).ConfigureAwait(false);

        if (authentication.Succeeded is false || authentication.Principal is null)
        {
            logger.LogWarning(
                authentication.Failure,
                "Agent authentication failed for {Path}: {FailureMessage}",
                context.Request.Path,
                authentication.Failure?.Message);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var principal = authentication.Principal;
        var requiredRole = configuration["Agent:Authentication:RequiredRole"];

        if (principal.Claims.Any(c => IsRoleClaim(c) && string.Equals(c.Value, requiredRole, StringComparison.Ordinal)) is false)
        {
            var roles = principal.Claims.Where(IsRoleClaim).Select(static claim => claim.Value).ToArray();
            logger.LogWarning(
                "Agent authorization rejected client {ClientId}: required role {RequiredRole} was not found. Token roles: {Roles}",
                principal.FindFirstValue("azp") ?? principal.FindFirstValue("appid"),
                requiredRole,
                roles);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var clientId = principal.FindFirstValue("azp") ?? principal.FindFirstValue("appid");
        var client = string.IsNullOrWhiteSpace(clientId)
            ? null
            : configuration.GetSection("Agent:Authentication:Clients").GetChildren().FirstOrDefault(
                section => string.Equals(section["ClientId"], clientId, StringComparison.OrdinalIgnoreCase));

        var botIdText = client?["BotId"];

        if (long.TryParse(botIdText, out var botId) is false || botId <= 0)
        {
            logger.LogWarning("Agent authorization rejected unconfigured client {ClientId}", clientId);
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

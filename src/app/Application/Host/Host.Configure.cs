using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GarageGroup.Internal.Timesheet;

partial class ApplicationHost
{
    internal const string AgentAuthenticationScheme = "TimesheetAgent";

    internal static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ValidateAgentConfiguration(builder.Configuration);

        _ = builder.Services
            .AddAuthentication()
            .AddJwtBearer(AgentAuthenticationScheme, options => ConfigureAgentJwt(options, builder.Configuration));
    }

    private static void ConfigureAgentJwt(JwtBearerOptions options, IConfiguration configuration)
    {
        var section = configuration.GetSection("Agent:Authentication");
        var tenantId = section["TenantId"];
        var audience = section["Audience"];

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(audience))
        {
            return;
        }

        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = audience;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuers =
            [
                $"https://sts.windows.net/{tenantId}/",
                $"https://login.microsoftonline.com/{tenantId}/v2.0"
            ],
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    private static void ValidateAgentConfiguration(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Agent:Enabled") is false)
        {
            return;
        }

        var section = configuration.GetSection("Agent:Authentication");

        RequireValue(section, "TenantId");
        RequireValue(section, "Audience");
        RequireValue(section, "RequiredRole");

        if (section.GetSection("Clients").GetChildren().GetEnumerator().MoveNext() is false)
        {
            throw new InvalidOperationException("Agent:Authentication:Clients must contain at least one trusted client mapping.");
        }
    }

    private static void RequireValue(IConfiguration section, string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            throw new InvalidOperationException($"Agent:Authentication:{key} must be configured when the agent API is enabled.");
        }
    }
}

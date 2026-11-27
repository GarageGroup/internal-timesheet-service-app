using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GarageGroup.Internal.Timesheet;

partial class ApplicationHost
{
    internal static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _ = builder.Services.AddTokenCredentialStandardAsSingleton();
    }
}

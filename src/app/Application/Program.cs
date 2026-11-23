using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace GarageGroup.Internal.Timesheet;

static class Program
{
    static Task Main(string[] args)
        =>
        AzureApplication.Create(args, ApplicationHost.Configure)
        .UseHealthCheck()
        .UseSwagger()
        .UseStandardSwaggerUI()
        .UseIsSuccessMiddleware()
        .UseAgentAuthentication()
        .UseLegacyJwtAuthentication()
        .UseProjectSetSearchEndpoint()
        .UseProjectSetGetEndpoint()
        .UseLastProjectSetGetEndpoint()
        .UseTimesheetSetGetEndpoint()
        .UseTimesheetModifyEndpointSet()
        .UseTimesheetDeleteEndpoint()
        .UseTagSetGetEndpoint()
        .UseNotificationEndpointSet()
        .UseSubscriptionSetGetEndpoint()
        .UseProfileGetEndpoint()
        .UseAgentActionDecideEndpoint()
        .UseAgentMessageSendEndpoint()
        .UseProfileUpdateEndpoint()
        .UseUserSignOutEndpoint()
        .UseUserSignInEndpoint()
        .UsePeriodSetGetEndpoint()
        .RunAsync();
}

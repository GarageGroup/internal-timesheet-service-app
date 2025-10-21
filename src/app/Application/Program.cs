using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace GarageGroup.Internal.Timesheet;

static class Program
{
    static Task Main(string[] args)
        =>
        AzureApplication.Create(args)
        .UseHealthCheck()
        .UseSwagger()
        .UseStandardSwaggerUI()
        .UseIsSuccessMiddleware()
        .UseJwtReader()
        .UseProjectSetSearchEndpoint()
        .UseProjectSetGetEndpoint()
        .UseLastProjectSetGetEndpoint()
        .UseTimesheetSetGetEndpoint()
        .UseTimesheetCreateEndpoint()
        .UseTimesheetDeleteEndpoint()
        .UseTimesheetUpdateEndpoint()
        .UseTagSetGetEndpoint()
        .UseNotificationSubscribeEndpoint()
        .UseNotificationUnsubscribeEndpoint()
        .UseSubscriptionSetGetEndpoint()
        .UseProfileGetEndpoint()
        .UseProfileUpdateEndpoint()
        .UseUserSignOutEndpoint()
        .UseUserSignInEndpoint()
        .UsePeriodSetGetEndpoint()
        .RunAsync();
}
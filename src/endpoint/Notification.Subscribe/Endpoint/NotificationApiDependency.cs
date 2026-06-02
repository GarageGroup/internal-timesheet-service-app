using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Notification.Subscribe.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class NotificationApiDependency
{
    public static Dependency<NotificationEndpointSet> UseNotificationEndpointSet<TBotApi>(
        this Dependency<IDataverseApiClient, TBotApi> dependency)
        where TBotApi : IBotInfoGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold(CreateApi).Map(NotificationEndpointSet.Resolve);

        static NotificationApi CreateApi(IDataverseApiClient dataverseApi, TBotApi botApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(botApi);
                
            return new(dataverseApi, botApi);
        }
    }
}
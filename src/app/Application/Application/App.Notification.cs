using System;
using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointSetApplicationExtension]
    internal static Dependency<NotificationEndpointSet> UseNotificationEndpointSet()
        =>
        Pipeline.Pipe(
            UseDataverseApi())
        .With(
            UseBotApi())
        .UseNotificationEndpointSet();
}
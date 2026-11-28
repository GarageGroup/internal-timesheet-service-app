using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.OpenPeriod.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class OpenPeriodApiDependency
{
    public static Dependency<IOpenPeriodApi> UseOpenPeriodApi<THttpMessageHandler>(
        this Dependency<THttpMessageHandler, WorkCalendarOption> dependency)
        where THttpMessageHandler : HttpMessageHandler
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IOpenPeriodApi>(CreateApi);

        static OpenPeriodApi CreateApi(THttpMessageHandler httpMessageHandler, WorkCalendarOption option)
        {
            ArgumentNullException.ThrowIfNull(httpMessageHandler);
            ArgumentNullException.ThrowIfNull(option);

            return new(httpMessageHandler, option, TodayProvider.Instance);
        }
    }
}

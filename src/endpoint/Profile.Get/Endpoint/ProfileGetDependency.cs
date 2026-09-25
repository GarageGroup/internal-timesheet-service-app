using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Profile.Get.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class ProfileGetDependency
{
    public static Dependency<IProfileGetFunc> UseProfileGetFunc<TSqlApi, TBotApi>(
        this Dependency<TSqlApi, TBotApi> dependency)
        where TSqlApi : ISqlQueryEntitySupplier
        where TBotApi : IBotInfoGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IProfileGetFunc>(CreateFunc);

        static ProfileGetFunc CreateFunc(TSqlApi dataverseApi, TBotApi botApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(botApi);

            return new(dataverseApi, botApi);
        }
    }

    public static Dependency<ProfileGetEndpoint> UseProfileGetEndpoint<TSqlApi, TBotApi>(
        this Dependency<TSqlApi, TBotApi> dependency)
        where TSqlApi : ISqlQueryEntitySupplier
        where TBotApi : IBotInfoGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.UseProfileGetFunc().Map(ProfileGetEndpoint.Resolve);
    }
}

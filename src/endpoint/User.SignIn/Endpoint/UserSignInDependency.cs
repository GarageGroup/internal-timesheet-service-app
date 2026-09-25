using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class UserSignInDependency
{
    public static Dependency<UserSignInEndpoint> UseUserSignInEndpoint<TBotApi>(
        this Dependency<IDataverseApiClient, TBotApi, UserSignInOption> dependency)
        where TBotApi : IBotInfoGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold(CreateFunc).Map(UserSignInEndpoint.Resolve);

        static UserSignInFunc CreateFunc(IDataverseApiClient dataverseApi, TBotApi botApi, UserSignInOption option)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(botApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(dataverseApi, botApi, new TelegramWebAppDataValidator(option, TimeProvider.System));
        }
    }
}

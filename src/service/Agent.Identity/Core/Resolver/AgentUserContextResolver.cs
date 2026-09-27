using GarageGroup.Infra;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentUserContextResolver(ISqlQueryEntitySetSupplier sqlApi) : IAgentUserContextResolver
{
    public ValueTask<Result<AgentUserContext, Failure<AgentUserContextResolveFailureCode>>> ResolveAsync(
        AgentUserIdentity identity,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            identity, cancellationToken)
        .Pipe(
            ValidateIdentity)
        .MapSuccess(
            static @in => DbAgentUserBinding.QueryAll with
            {
                Top = 2,
                Filter = DbAgentUserBinding.BuildFilter(@in.BotId, @in.TelegramUserId)
            })
        .ForwardValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbAgentUserBinding>,
            static failure => failure.WithFailureCode(AgentUserContextResolveFailureCode.Unknown))
        .Map(
            bindings => MapBindings(identity, bindings),
            static failure => failure)
        .Forward(
            static result => result);

    private static Result<AgentUserIdentity, Failure<AgentUserContextResolveFailureCode>> ValidateIdentity(
        AgentUserIdentity identity)
    {
        if (identity.BotId <= 0 || identity.TelegramUserId <= 0 || identity.TelegramChatId == 0)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.InvalidIdentity, "Agent user identity is invalid");
        }

        if (identity.TelegramChatId != identity.TelegramUserId)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.UnsupportedChat, "Only private Telegram chats are supported");
        }

        return identity;
    }

    private static Result<AgentUserContext, Failure<AgentUserContextResolveFailureCode>> MapBindings(
        AgentUserIdentity identity,
        FlatArray<DbAgentUserBinding> bindings)
    {
        var bindingArray = bindings.AsEnumerable().Take(2).ToArray();

        if (bindingArray.Length is 0)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.UserNotLinked, "Telegram user is not linked");
        }

        if (bindingArray.Length > 1)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.AmbiguousBinding, "Several active user bindings were found");
        }

        var binding = bindingArray[0];
        if (binding.IsSignedOut)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.BindingSignedOut, "User binding is signed out");
        }

        if (binding.IsUserDisabled)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.UserDisabled, "CRM user is disabled");
        }

        if (binding.EntraObjectId is not { } entraObjectId || entraObjectId == Guid.Empty)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.MissingEntraObjectId, "CRM user has no Entra object ID");
        }

        if (binding.BindingId == Guid.Empty || binding.CrmSystemUserId == Guid.Empty)
        {
            return Failure.Create(AgentUserContextResolveFailureCode.Unknown, "User binding contains invalid identifiers");
        }

        return new AgentUserContext(
            botId: identity.BotId,
            telegramUserId: identity.TelegramUserId,
            telegramChatId: identity.TelegramChatId,
            bindingId: binding.BindingId,
            crmSystemUserId: binding.CrmSystemUserId,
            entraObjectId: entraObjectId);
    }
}

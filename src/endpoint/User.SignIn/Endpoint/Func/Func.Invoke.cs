using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class UserSignInFunc
{
    public ValueTask<Result<Unit, Failure<UserSignInFailureCode>>> InvokeAsync(
        UserSignInIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            MapTelegramDataOrFailure)
        .ForwardValue(
            InnerInvokeAsync);

    private ValueTask<Result<Unit, Failure<UserSignInFailureCode>>> InnerInvokeAsync(
        UserChatId input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeParallelValue(
            GetSystemUserAsync,
            GetBotInfoAsync)
        .MapSuccess(
            @out => new SignInContext
            {
                User = input,
                SystemUser = @out.Item1,
                Bot = @out.Item2
            })
        .ForwardValue(
            EnsureBindingAvailableAsync)
        .MapSuccess(
            static context => UserJson.BuildDataverseInput(
                systemUserId: context.User.SystemUserId,
                botId: context.Bot.Id,
                user: new()
                {
                    BotId = context.Bot.Id,
                    BotName = $"{context.Bot.Username} - {context.SystemUser.FullName}",
                    ChatId = context.User.ChatId,
                    UserLookupValue = UserJson.BuildUserLookupValue(context.User.SystemUserId),
                    IsSignedOut = false
                }))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.WithFailureCode(UserSignInFailureCode.Unknown));

    private ValueTask<Result<SignInContext, Failure<UserSignInFailureCode>>> EnsureBindingAvailableAsync(
        SignInContext context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            UserBindingJson.BuildDataverseInput(context.Bot.Id, context.User.ChatId),
            cancellationToken)
        .PipeValue(
            dataverseApi.GetEntitySetAsync<UserBindingJson>)
        .Map(
            @out => ValidateBindings(context, @out.Value),
            static failure => failure.WithFailureCode(UserSignInFailureCode.Unknown))
        .Forward(
            static result => result);

    private static Result<SignInContext, Failure<UserSignInFailureCode>> ValidateBindings(
        SignInContext context,
        FlatArray<UserBindingJson> bindings)
    {
        var bindingArray = bindings.AsEnumerable().Take(2).ToArray();
        if (bindingArray.Length > 1)
        {
            return Failure.Create(
                UserSignInFailureCode.TelegramUserAlreadyLinked,
                "Several active Telegram user bindings were found");
        }

        if (bindingArray is [{ CrmSystemUserId: var crmSystemUserId }] && crmSystemUserId != context.User.SystemUserId)
        {
            return Failure.Create(
                UserSignInFailureCode.TelegramUserAlreadyLinked,
                "Telegram user is already linked to another system user");
        }

        return context;
    }

    private ValueTask<Result<SystemUserJson, Failure<UserSignInFailureCode>>> GetSystemUserAsync(
        UserChatId input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input.SystemUserId, cancellationToken)
        .Pipe(
            SystemUserJson.BuildDataverseInput)
        .PipeValue(
            dataverseApi.GetEntityAsync<SystemUserJson>)
        .Map(
            static success => success.Value,
            static failure => failure.MapFailureCode(ToUserSignInFailureCode));

    private ValueTask<Result<BotInfoGetOut, Failure<UserSignInFailureCode>>> GetBotInfoAsync(
        UserChatId _, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe<Unit>(
            default, cancellationToken)
        .PipeValue(
            botApi.GetBotInfoAsync)
        .MapFailure(
            static failure => failure.WithFailureCode(UserSignInFailureCode.Unknown));

    private Result<UserChatId, Failure<UserSignInFailureCode>> MapTelegramDataOrFailure(UserSignInIn input)
        =>
        telegramDataValidator.Validate(input.TelegramData).MapSuccess(
            chatId => new UserChatId
            {
                ChatId = chatId,
                SystemUserId = input.SystemUserId
            });
}

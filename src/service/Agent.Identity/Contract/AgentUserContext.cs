using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentUserContext
{
    public AgentUserContext(
        long botId,
        long telegramUserId,
        long telegramChatId,
        Guid bindingId,
        Guid crmSystemUserId,
        Guid entraObjectId)
    {
        BotId = botId;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
        BindingId = bindingId;
        CrmSystemUserId = crmSystemUserId;
        EntraObjectId = entraObjectId;
    }

    public long BotId { get; }

    public long TelegramUserId { get; }

    public long TelegramChatId { get; }

    public Guid BindingId { get; }

    public Guid CrmSystemUserId { get; }

    public Guid EntraObjectId { get; }
}

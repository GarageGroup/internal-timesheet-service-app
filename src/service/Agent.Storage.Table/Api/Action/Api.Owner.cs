using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionTableApi
{
    private static bool IsAnotherOwner(TableEntity entity, AgentUserContext context)
        =>
        entity.GetInt64(TelegramUserIdPropertyName).Equals(context.TelegramUserId) is false ||
        entity.GetInt64(TelegramChatIdPropertyName).Equals(context.TelegramChatId) is false ||
        entity.GetGuid(BindingIdPropertyName).Equals(context.BindingId) is false ||
        entity.GetGuid(CrmSystemUserIdPropertyName).Equals(context.CrmSystemUserId) is false ||
        entity.GetGuid(EntraObjectIdPropertyName).Equals(context.EntraObjectId) is false;
}

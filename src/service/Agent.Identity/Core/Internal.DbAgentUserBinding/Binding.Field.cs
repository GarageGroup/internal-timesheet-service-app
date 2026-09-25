using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

partial record class DbAgentUserBinding
{
    [DbSelect(All, BindingAlias, $"{BindingAlias}.gg_telegram_bot_userid")]
    public Guid BindingId { get; init; }

    [DbSelect(All, BindingAlias, $"{BindingAlias}.gg_systemuser_id")]
    public Guid CrmSystemUserId { get; init; }

    [DbSelect(All, BindingAlias, $"{BindingAlias}.gg_is_user_signed_out")]
    public bool IsSignedOut { get; init; }

    [DbSelect(All, UserAlias, $"{UserAlias}.azureactivedirectoryobjectid")]
    public Guid? EntraObjectId { get; init; }

    [DbSelect(All, UserAlias, $"{UserAlias}.isdisabled")]
    public bool IsUserDisabled { get; init; }
}

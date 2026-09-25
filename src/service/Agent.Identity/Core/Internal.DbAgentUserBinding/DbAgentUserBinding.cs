using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[DbEntity("gg_telegram_bot_user", BindingAlias)]
[DbJoin(DbJoinType.Left, "systemuser", UserAlias, $"{BindingAlias}.gg_systemuser_id = {UserAlias}.systemuserid")]
internal sealed partial record class DbAgentUserBinding : IDbEntity<DbAgentUserBinding>
{
    private const string All = "QueryAll";

    private const string BindingAlias = "b";

    private const string UserAlias = "u";
}

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentReadPlugin(
    AgentUserContext context,
    IAgentTimesheetSetGetFunc timesheetSetGetFunc,
    IAgentProjectSetSearchFunc projectSetSearchFunc,
    IAgentLastProjectSetGetFunc lastProjectSetGetFunc,
    IAgentPeriodSetGetFunc periodSetGetFunc,
    IAgentTagSetGetFunc tagSetGetFunc)
{
    internal const string PluginName = "TimesheetRead";
}

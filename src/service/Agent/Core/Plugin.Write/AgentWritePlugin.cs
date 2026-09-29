namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentWritePlugin(
    AgentUserContext context,
    IAgentTimesheetCreatePrepareFunc timesheetCreatePrepareFunc,
    AgentPreparedActionCapture actionCapture)
{
    internal const string PluginName = "TimesheetWritePreparation";
}

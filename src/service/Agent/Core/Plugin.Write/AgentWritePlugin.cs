namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentWritePlugin(
    AgentUserContext context,
    IAgentTimesheetCreatePrepareFunc timesheetCreatePrepareFunc,
    IAgentTimesheetDeletePrepareFunc timesheetDeletePrepareFunc,
    AgentPreparedActionCapture actionCapture)
{
    internal const string PluginName = "TimesheetWritePreparation";
}

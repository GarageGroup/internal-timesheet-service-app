using System;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

public sealed class AgentKernelScope
{
    private readonly AgentPreparedActionCapture actionCapture;

    internal AgentKernelScope(Kernel kernel, AgentPreparedActionCapture actionCapture)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(actionCapture);

        Kernel = kernel;
        this.actionCapture = actionCapture;
    }

    public Kernel Kernel { get; }

    public AgentTimesheetCreatePrepareOut? PreparedCreateAction
        =>
        actionCapture.CreateAction;

    public AgentTimesheetDeletePrepareOut? PreparedDeleteAction
        =>
        actionCapture.DeleteAction;

    public AgentTimesheetUpdatePrepareOut? PreparedUpdateAction
        =>
        actionCapture.UpdateAction;
}

using System.Threading;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentPreparedActionCapture
{
    private int state;

    internal AgentTimesheetCreatePrepareOut? CreateAction { get; private set; }

    internal AgentTimesheetDeletePrepareOut? DeleteAction { get; private set; }

    internal AgentTimesheetUpdatePrepareOut? UpdateAction { get; private set; }

    internal bool TryStart()
        =>
        Interlocked.CompareExchange(ref state, 1, 0) is 0;

    internal void Complete(AgentTimesheetCreatePrepareOut action)
    {
        CreateAction = action;
        _ = Interlocked.Exchange(ref state, 2);
    }

    internal void Complete(AgentTimesheetDeletePrepareOut action)
    {
        DeleteAction = action;
        _ = Interlocked.Exchange(ref state, 2);
    }

    internal void Complete(AgentTimesheetUpdatePrepareOut action)
    {
        UpdateAction = action;
        _ = Interlocked.Exchange(ref state, 2);
    }

    internal void Reset()
        =>
        _ = Interlocked.CompareExchange(ref state, 0, 1);
}

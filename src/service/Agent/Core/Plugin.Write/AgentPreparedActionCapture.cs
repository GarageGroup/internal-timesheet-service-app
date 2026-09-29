using System.Threading;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentPreparedActionCapture
{
    private int state;

    internal AgentTimesheetCreatePrepareOut? Action { get; private set; }

    internal bool TryStart()
        =>
        Interlocked.CompareExchange(ref state, 1, 0) is 0;

    internal void Complete(AgentTimesheetCreatePrepareOut action)
    {
        Action = action;
        _ = Interlocked.Exchange(ref state, 2);
    }

    internal void Reset()
        =>
        _ = Interlocked.CompareExchange(ref state, 0, 1);
}

using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentKernelToolSet
{
    public AgentKernelToolSet(
        IAgentTimesheetSetGetFunc timesheetSetGetFunc,
        IAgentProjectSetSearchFunc projectSetSearchFunc,
        IAgentLastProjectSetGetFunc lastProjectSetGetFunc,
        IAgentPeriodSetGetFunc periodSetGetFunc,
        IAgentTagSetGetFunc tagSetGetFunc,
        IAgentTimesheetCreatePrepareFunc timesheetCreatePrepareFunc,
        IAgentTimesheetDeletePrepareFunc timesheetDeletePrepareFunc)
    {
        ArgumentNullException.ThrowIfNull(timesheetSetGetFunc);
        ArgumentNullException.ThrowIfNull(projectSetSearchFunc);
        ArgumentNullException.ThrowIfNull(lastProjectSetGetFunc);
        ArgumentNullException.ThrowIfNull(periodSetGetFunc);
        ArgumentNullException.ThrowIfNull(tagSetGetFunc);
        ArgumentNullException.ThrowIfNull(timesheetCreatePrepareFunc);
        ArgumentNullException.ThrowIfNull(timesheetDeletePrepareFunc);

        TimesheetSetGetFunc = timesheetSetGetFunc;
        ProjectSetSearchFunc = projectSetSearchFunc;
        LastProjectSetGetFunc = lastProjectSetGetFunc;
        PeriodSetGetFunc = periodSetGetFunc;
        TagSetGetFunc = tagSetGetFunc;
        TimesheetCreatePrepareFunc = timesheetCreatePrepareFunc;
        TimesheetDeletePrepareFunc = timesheetDeletePrepareFunc;
    }

    public IAgentTimesheetSetGetFunc TimesheetSetGetFunc { get; }

    public IAgentProjectSetSearchFunc ProjectSetSearchFunc { get; }

    public IAgentLastProjectSetGetFunc LastProjectSetGetFunc { get; }

    public IAgentPeriodSetGetFunc PeriodSetGetFunc { get; }

    public IAgentTagSetGetFunc TagSetGetFunc { get; }

    public IAgentTimesheetCreatePrepareFunc TimesheetCreatePrepareFunc { get; }

    public IAgentTimesheetDeletePrepareFunc TimesheetDeletePrepareFunc { get; }
}

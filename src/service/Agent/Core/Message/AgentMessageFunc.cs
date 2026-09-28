using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentMessageFunc(
    IAgentKernelFactory kernelFactory,
    IDateProvider dateProvider,
    AgentMessageOption option) : IAgentMessageFunc;

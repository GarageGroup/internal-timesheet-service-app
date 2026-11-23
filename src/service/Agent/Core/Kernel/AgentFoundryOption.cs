using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentFoundryOption(Uri ProjectEndpoint, string ModelId, string TokenScope);

using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentActionTableOption(Uri ServiceEndpoint, string TableName);

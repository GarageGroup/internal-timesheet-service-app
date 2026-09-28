using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentRequestTableOption(Uri ServiceEndpoint, string TableName);

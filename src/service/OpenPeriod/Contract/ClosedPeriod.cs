using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class ClosedPeriod(string Name, DateOnly From, DateOnly To);

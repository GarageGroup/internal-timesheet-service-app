using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct ProfileGetIn
{
    public ProfileGetIn([ClaimIn("oid")] Guid systemUserId)
        =>
        SystemUserId = systemUserId;

    public Guid SystemUserId { get; }
}
using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

internal interface IProjectDataverseInputBuilder
{
    static abstract DataverseEntityGetIn BuildDataverseEntityGetIn(Guid projectId, Guid callerObjectId);
}
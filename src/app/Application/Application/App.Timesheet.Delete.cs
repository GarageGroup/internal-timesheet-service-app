using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<TimesheetDeleteEndpoint> UseTimesheetDeleteEndpoint()
        =>
        UseDataverseApi().With(UseOpenPeriodApi()).UseTimesheetDeleteEndpoint();
}
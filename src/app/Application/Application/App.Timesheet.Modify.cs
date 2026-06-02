using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointSetApplicationExtension]
    internal static Dependency<TimesheetModifyEndpointSet> UseTimesheetModifyEndpointSet()
        =>
        UseDataverseApi().UseTimesheetModifyEndpointSet();
}
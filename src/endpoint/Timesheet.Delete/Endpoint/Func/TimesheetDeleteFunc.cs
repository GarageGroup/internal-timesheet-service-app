using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TimesheetDeleteFunc(IDataverseEntityDeleteSupplier dataverseApi) : ITimesheetDeleteFunc;
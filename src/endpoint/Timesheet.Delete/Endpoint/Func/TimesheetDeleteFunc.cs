using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TimesheetDeleteFunc(IDataverseApiClient dataverseApi, IOpenPeriodApi openPeriodApi) : ITimesheetDeleteFunc;
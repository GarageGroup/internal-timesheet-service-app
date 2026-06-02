using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[EndpointSet]
public interface ITimesheetModifyApi : ITimesheetCreateFunc, ITimesheetUpdateFunc;
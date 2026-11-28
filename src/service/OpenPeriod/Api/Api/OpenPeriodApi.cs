using System;
using System.Net.Http;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class OpenPeriodApi(
    HttpMessageHandler httpMessageHandler, WorkCalendarOption option, ITodayProvider todayProvider) : IOpenPeriodApi
{
    // The number of working days after the end of a period when its time entries may still be created, changed and deleted
    internal const int OpenWorkingDays = 3;

    // The working days after the end of a period are over by this day in any calendar, so the calendar is not requested
    private const int MaxOpenDays = 31;

    private const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";

    private const string DateFormat = "yyyy-MM-dd";

    // The calendar must not delay an action for long: when it does not answer in time, Monday - Friday are working days
    private static readonly TimeSpan CalendarTimeout = TimeSpan.FromSeconds(5);
}

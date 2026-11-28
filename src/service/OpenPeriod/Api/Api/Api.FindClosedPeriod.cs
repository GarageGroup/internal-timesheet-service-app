using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class OpenPeriodApi
{
    public async ValueTask<ClosedPeriod?> FindClosedPeriodAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var today = todayProvider.EarliestToday;

        var from = new DateOnly(date.Year, date.Month, 1);
        if (from > today)
        {
            return null;
        }

        // The period stays open through the working day number OpenWorkingDays after its end,
        // and that day cannot be earlier than the same number of calendar days
        var to = from.AddMonths(1).AddDays(-1);
        if (today <= to.AddDays(OpenWorkingDays))
        {
            return null;
        }

        var period = new ClosedPeriod(
            Name: date.ToString("yy_MM", CultureInfo.InvariantCulture),
            From: from,
            To: to);

        if (today > to.AddDays(MaxOpenDays))
        {
            return period;
        }

        using var httpClient = CreateHttpClient();
        var isCalendarAvailable = httpClient is not null;
        var workingDays = 0;

        // The period is closed when all its open working days have passed before today
        for (var day = to.AddDays(1); day < today; day = day.AddDays(1))
        {
            bool? isWorkingDay = null;
            if (isCalendarAvailable)
            {
                var calendarDay = await GetCalendarDayAsync(httpClient!, day, cancellationToken).ConfigureAwait(false);

                // After the first failure the calendar is not requested again for this check
                isCalendarAvailable = calendarDay.IsCalendarAvailable;
                isWorkingDay = calendarDay.IsWorkingDay;
            }

            if ((isWorkingDay ?? IsWeekday(day)) && ++workingDays >= OpenWorkingDays)
            {
                return period;
            }
        }

        return null;
    }

    private HttpClient? CreateHttpClient()
    {
        if (option.BaseAddress is null)
        {
            return null;
        }

        var baseAddress = option.BaseAddress.AbsoluteUri;
        var httpClient = new HttpClient(httpMessageHandler, disposeHandler: false)
        {
            BaseAddress = new(baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/"),
            Timeout = CalendarTimeout
        };

        if (string.IsNullOrWhiteSpace(option.SubscriptionKey) is false)
        {
            httpClient.DefaultRequestHeaders.Add(SubscriptionKeyHeaderName, option.SubscriptionKey);
        }

        return httpClient;
    }

    // A day the calendar does not have is not a failure of the calendar: only this day is taken by the day of week
    private async ValueTask<(bool IsCalendarAvailable, bool? IsWorkingDay)> GetCalendarDayAsync(
        HttpClient httpClient, DateOnly day, CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = Uri.EscapeDataString(option.Country) + "/" + day.ToString(DateFormat, CultureInfo.InvariantCulture);
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return (true, null);
            }

            if (response.IsSuccessStatusCode is false)
            {
                return (false, null);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind is JsonValueKind.Object &&
                document.RootElement.TryGetProperty("isWorkingDay", out var isWorkingDay) &&
                isWorkingDay.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return (true, isWorkingDay.GetBoolean());
            }

            return (false, null);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException ||
            (exception is OperationCanceledException && cancellationToken.IsCancellationRequested is false))
        {
            return (false, null);
        }
    }

    private static bool IsWeekday(DateOnly day)
        =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}

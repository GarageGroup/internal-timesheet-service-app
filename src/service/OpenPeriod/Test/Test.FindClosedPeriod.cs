using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.OpenPeriod.Test;

partial class OpenPeriodApiTest
{
    [Theory]
    // The current period
    [InlineData("2026-11-01", "2026-11-28")]
    [InlineData("2026-11-28", "2026-11-28")]
    [InlineData("2026-11-10", "2026-11-30")]
    // A period that has not started yet
    [InlineData("2026-12-05", "2026-11-28")]
    [InlineData("9999-12-31", "2026-11-28")]
    // Three working days cannot pass in three calendar days after the end of the period
    [InlineData("2026-10-31", "2026-11-01")]
    [InlineData("2026-10-01", "2026-11-03")]
    [InlineData("2025-12-15", "2026-01-03")]
    public static async Task FindClosedPeriodAsync_PeriodIsOpenWhateverTheCalendarIs_ExpectNullAndCalendarIsNotRequested(
        string date, string earliestToday)
    {
        var handler = BuildFailedCalendar();
        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(ParseDate(date), TestContext.Current.CancellationToken);

        Assert.Null(actual);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("2026-10-31", "2026-12-02", "26_10", "2026-10-01", "2026-10-31")]
    [InlineData("2026-09-30", "2026-11-28", "26_09", "2026-09-01", "2026-09-30")]
    [InlineData("2024-02-10", "2026-11-28", "24_02", "2024-02-01", "2024-02-29")]
    [InlineData("2021-10-07", "2026-11-28", "21_10", "2021-10-01", "2021-10-31")]
    public static async Task FindClosedPeriodAsync_PeriodHasEndedLongAgo_ExpectClosedPeriodAndCalendarIsNotRequested(
        string date, string earliestToday, string expectedName, string expectedFrom, string expectedTo)
    {
        var handler = BuildFailedCalendar();
        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(ParseDate(date), TestContext.Current.CancellationToken);

        Assert.Equal(new ClosedPeriod(expectedName, ParseDate(expectedFrom), ParseDate(expectedTo)), actual);
        Assert.Empty(handler.Requests);
    }

    // After October 2026 the calendar has 2026-11-01 (Sunday) and 2026-11-04 (a holiday on Wednesday) as days off,
    // so the three working days are November 2, 3 and 5
    [Theory]
    [InlineData("2026-11-04")]
    [InlineData("2026-11-05")]
    public static async Task FindClosedPeriodAsync_ThirdWorkingDayOfCalendarHasNotPassed_ExpectNull(string earliestToday)
    {
        var handler = BuildCalendar("2026-11-01", "2026-11-04");
        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData("2026-11-06")]
    [InlineData("2026-11-07")]
    [InlineData("2026-11-30")]
    public static async Task FindClosedPeriodAsync_ThirdWorkingDayOfCalendarHasPassed_ExpectClosedPeriod(string earliestToday)
    {
        var handler = BuildCalendar("2026-11-01", "2026-11-04");
        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(October2026, actual);
        Assert.Equal(["2026-11-01", "2026-11-02", "2026-11-03", "2026-11-04", "2026-11-05"], GetRequestedDays(handler));
    }

    // The holidays of January 2026 by the calendar: the working days after December are January 12, 13 and 14
    [Theory]
    [InlineData("2026-01-09", false)]
    [InlineData("2026-01-14", false)]
    [InlineData("2026-01-15", true)]
    public static async Task FindClosedPeriodAsync_HolidaysAfterTheEndOfPeriod_ExpectHolidaysAreNotCounted(
        string earliestToday, bool isClosed)
    {
        var handler = BuildCalendar(
            "2026-01-01", "2026-01-02", "2026-01-03", "2026-01-04", "2026-01-05", "2026-01-06",
            "2026-01-07", "2026-01-08", "2026-01-09", "2026-01-10", "2026-01-11");

        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(new(2025, 12, 31), TestContext.Current.CancellationToken);

        Assert.Equal(isClosed ? new ClosedPeriod("25_12", new(2025, 12, 01), new(2025, 12, 31)) : null, actual);
    }

    // A weekend day the calendar has as a working one is counted: Sunday November 1 is working here
    [Fact]
    public static async Task FindClosedPeriodAsync_CalendarHasWorkingWeekendDay_ExpectTheDayIsCounted()
    {
        var handler = BuildCalendar();
        var api = BuildApi(handler, "2026-11-04");

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(October2026, actual);
    }

    [Fact]
    public static async Task FindClosedPeriodAsync_ExpectDayIsRequestedByCountryAndDateWithSubscriptionKey()
    {
        var handler = BuildCalendar();

        var option = new WorkCalendarOption
        {
            BaseAddress = new("https://calendar.test/production-calendar"),
            SubscriptionKey = "some-key",
            Country = "KZ"
        };

        var api = BuildApi(handler, "2026-11-05", option);
        _ = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                (new Uri("https://calendar.test/production-calendar/KZ/2026-11-01"), "some-key"),
                (new Uri("https://calendar.test/production-calendar/KZ/2026-11-02"), "some-key"),
                (new Uri("https://calendar.test/production-calendar/KZ/2026-11-03"), "some-key")
            ],
            handler.Requests);
    }

    // Without the calendar Monday - Friday are working days: after October 2026 they are November 2, 3 and 4
    [Theory]
    [InlineData("2026-11-04", false)]
    [InlineData("2026-11-05", true)]
    public static async Task FindClosedPeriodAsync_CalendarAddressIsAbsent_ExpectWeekdaysAreWorkingAndCalendarIsNotRequested(
        string earliestToday, bool isClosed)
    {
        var handler = BuildCalendar("2026-11-02", "2026-11-03", "2026-11-04");
        var api = BuildApi(handler, earliestToday, new WorkCalendarOption());

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(isClosed ? October2026 : null, actual);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "2026-11-04", false)]
    [InlineData(HttpStatusCode.InternalServerError, "2026-11-05", true)]
    [InlineData(HttpStatusCode.Unauthorized, "2026-11-05", true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "2026-11-05", true)]
    [InlineData(HttpStatusCode.BadRequest, "2026-11-04", false)]
    public static async Task FindClosedPeriodAsync_CalendarResponseIsFailure_ExpectWeekdaysAreWorkingAndCalendarIsRequestedOnce(
        HttpStatusCode statusCode, string earliestToday, bool isClosed)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{"error":"Some error"}""", statusCode));
        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(isClosed ? October2026 : null, actual);
        Assert.Equal(["2026-11-01"], GetRequestedDays(handler));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Some text")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"isWorkingDay":"yes"}""")]
    public static async Task FindClosedPeriodAsync_CalendarResponseIsUnexpected_ExpectWeekdaysAreWorkingAndCalendarIsRequestedOnce(
        string content)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(content));
        var api = BuildApi(handler, "2026-11-05");

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(October2026, actual);
        Assert.Equal(["2026-11-01"], GetRequestedDays(handler));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static async Task FindClosedPeriodAsync_CalendarRequestFailsOrTimesOut_ExpectWeekdaysAreWorkingAndCalendarIsRequestedOnce(
        bool isTimeout)
    {
        var handler = new StubHttpMessageHandler(
            _ => throw (isTimeout ? new TaskCanceledException("Some timeout") : new HttpRequestException("Some error")));

        var api = BuildApi(handler, "2026-11-05");

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(October2026, actual);
        Assert.Single(handler.Requests);
    }

    // The calendar has no day of November 2026 except the holiday, so the other days are taken by the day of week,
    // and the calendar is still requested for every day
    [Theory]
    [InlineData("2026-11-05", false)]
    [InlineData("2026-11-06", true)]
    public static async Task FindClosedPeriodAsync_CalendarHasNoDay_ExpectOnlyThatDayIsTakenByTheDayOfWeek(
        string earliestToday, bool isClosed)
    {
        var handler = new StubHttpMessageHandler(
            static request => request.RequestUri!.Segments[^1] is "2026-11-04"
                ? StubHttpMessageHandler.Json("""{"date":"2026-11-04","country":"RU","isWorkingDay":false,"dayType":"Holiday"}""")
                : StubHttpMessageHandler.Json("""{"error":"Calendar day not found"}""", HttpStatusCode.NotFound));

        var api = BuildApi(handler, earliestToday);

        var actual = await api.FindClosedPeriodAsync(new(2026, 10, 20), TestContext.Current.CancellationToken);

        Assert.Equal(isClosed ? October2026 : null, actual);
        Assert.Equal(Enumerable.Range(1, isClosed ? 5 : 4).Select(static day => $"2026-11-0{day}"), GetRequestedDays(handler));
    }

    [Fact]
    public static async Task FindClosedPeriodAsync_CallerCancelsTheRequest_ExpectOperationCanceledException()
    {
        using var cancellationTokenSource = new System.Threading.CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var handler = new StubHttpMessageHandler(static _ => throw new TaskCanceledException());
        var api = BuildApi(handler, "2026-11-05");

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await api.FindClosedPeriodAsync(new(2026, 10, 20), cancellationTokenSource.Token));
    }
}

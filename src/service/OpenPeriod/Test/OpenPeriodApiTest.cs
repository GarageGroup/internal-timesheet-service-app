using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.OpenPeriod.Test;

public static partial class OpenPeriodApiTest
{
    private static readonly WorkCalendarOption SomeOption
        =
        new()
        {
            BaseAddress = new("https://calendar.test/production-calendar"),
            SubscriptionKey = "some-key",
            Country = "RU"
        };

    private static readonly ClosedPeriod October2026
        =
        new("26_10", new(2026, 10, 01), new(2026, 10, 31));

    private static OpenPeriodApi BuildApi(HttpMessageHandler handler, string earliestToday, WorkCalendarOption? option = null)
        =>
        new(handler, option ?? SomeOption, Mock.Of<ITodayProvider>(t => t.EarliestToday == ParseDate(earliestToday)));

    // The calendar has only the days off; any other day is a working one
    private static StubHttpMessageHandler BuildCalendar(params string[] daysOff)
        =>
        new(
            request =>
            {
                var date = request.RequestUri!.Segments[^1];
                return StubHttpMessageHandler.Json(
                    daysOff.Contains(date)
                        ? $$"""{"date":"{{date}}","country":"RU","isWorkingDay":false,"dayType":"Holiday","comment":null}"""
                        : $$"""{"date":"{{date}}","country":"RU","isWorkingDay":true,"dayType":"WorkingDay"}""");
            });

    private static StubHttpMessageHandler BuildFailedCalendar()
        =>
        new(static _ => StubHttpMessageHandler.Json("""{"error":"Some error"}""", HttpStatusCode.InternalServerError));

    private static IEnumerable<string> GetRequestedDays(StubHttpMessageHandler handler)
        =>
        handler.Requests.Select(static request => request.Uri.Segments[^1]);

    private static DateOnly ParseDate(string date)
        =>
        DateOnly.ParseExact(date, "yyyy-MM-dd");
}

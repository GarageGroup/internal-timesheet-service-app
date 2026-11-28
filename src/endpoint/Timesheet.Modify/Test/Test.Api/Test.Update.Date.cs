using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyApiTest
{
    private static TimesheetUpdateIn BuildUpdateInput(DateOnly? date)
        =>
        new(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: date,
            project: null,
            duration: 3,
            description: "Some description");

    private static DataverseEntityGetOut<TimesheetDateJson> BuildTimesheetDateJsonOut(DateOnly? date)
        =>
        new(
            value: new()
            {
                Date = date
            });

    [Fact]
    public static async Task UpdateAsync_ExpectTimesheetDateIsRequestedOnceOnBehalfOfTheUser()
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        _ = await api.UpdateAsync(BuildUpdateInput(new(2024, 06, 07)), TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntityGetIn(
            entityPluralName: "gg_timesheetactivities",
            entityKey: new DataversePrimaryKey(new("80108b86-61ae-47ea-bd61-6d0c126a42b4")),
            selectFields: ["gg_date"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<TimesheetDateJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetUpdateFailureCode.TimesheetNotFound)]
    public static async Task UpdateAsync_TimesheetDateGetResultIsFailure_ExpectFailureAndTimesheetIsNotUpdated(
        DataverseFailureCode sourceFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, dataverseFailure);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var actual = await api.UpdateAsync(BuildUpdateInput(new(2024, 06, 07)), TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(2026, 11, 29)]
    [InlineData(2027, 03, 01)]
    public static async Task UpdateAsync_InputDateIsInTheFuture_ExpectFutureDateFailureAndTimesheetIsNotUpdated(
        int year, int month, int day)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.UpdateAsync(BuildUpdateInput(new(year, month, day)), TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetUpdateFailureCode.FutureDate, $"Time cannot be logged for a future date: {year:0000}-{month:00}-{day:00}.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public static async Task UpdateAsync_InputDateIsToday_ExpectDataverseUpdateCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.UpdateAsync(BuildUpdateInput(new(2026, 11, 28)), TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // The current date of the entry is 2024-06-05
    [Theory]
    [InlineData(
        "2024-07-10",
        "statecode eq 1 and ((gg_from_date le 2024-06-05 and gg_to_date ge 2024-06-05) or (gg_from_date le 2024-07-10 and gg_to_date ge 2024-07-10))")]
    [InlineData("2024-06-05", "statecode eq 1 and ((gg_from_date le 2024-06-05 and gg_to_date ge 2024-06-05))")]
    [InlineData(null, "statecode eq 1 and ((gg_from_date le 2024-06-05 and gg_to_date ge 2024-06-05))")]
    public static async Task UpdateAsync_ExpectClosedPeriodOfCurrentAndNewDatesIsRequestedOnce(string? newDate, string expectedFilter)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var input = BuildUpdateInput(newDate is null ? null : DateOnly.ParseExact(newDate, "yyyy-MM-dd"));
        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_employee_cost_periods",
            selectFields: ["gg_name", "gg_from_date", "gg_to_date"],
            filter: expectedFilter,
            top: 1);

        mockDataverseApi.Verify(a => a.GetEntitySetAsync<PeriodJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_TimesheetHasNoDateAndInputDateIsAbsent_ExpectPeriodIsNotRequestedAndTimesheetIsUpdated()
    {
        var mockDataverseApi = BuildMockDataverseApi(ClosedPeriodJsonOut, BuildTimesheetDateJsonOut(null));
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var actual = await api.UpdateAsync(BuildUpdateInput(null), TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // The closed period is May 2024
    [Theory]
    [InlineData("2024-05-20", null, "2024-05-20")]
    [InlineData("2024-05-20", "2024-06-10", "2024-05-20")]
    [InlineData("2024-06-05", "2024-05-31", "2024-05-31")]
    [InlineData("2024-05-01", "2024-05-02", "2024-05-01")]
    public static async Task UpdateAsync_PeriodOfCurrentOrNewDateIsClosed_ExpectPeriodClosedFailureAndTimesheetIsNotUpdated(
        string currentDate, string? newDate, string expectedDate)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            ClosedPeriodJsonOut, BuildTimesheetDateJsonOut(DateOnly.ParseExact(currentDate, "yyyy-MM-dd")));

        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var input = BuildUpdateInput(newDate is null ? null : DateOnly.ParseExact(newDate, "yyyy-MM-dd"));
        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetUpdateFailureCode.PeriodClosed,
            $"The period 24_05 (2024-05-01 - 2024-05-31) is closed: time entries of {expectedDate} cannot be created or changed.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    public static async Task UpdateAsync_PeriodGetResultIsFailure_ExpectUnknownFailureAndTimesheetIsNotUpdated(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var actual = await api.UpdateAsync(BuildUpdateInput(new(2024, 06, 07)), TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetUpdateFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

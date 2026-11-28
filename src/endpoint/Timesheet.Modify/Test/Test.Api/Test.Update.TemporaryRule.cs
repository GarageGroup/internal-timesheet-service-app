using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyApiTest
{
    // October 2026 is closed by the temporary rule, November is open
    [Theory]
    // An entry of a closed period cannot be changed
    [InlineData("2026-10-20", null, "26_10 (2026-10-01 - 2026-10-31)", "2026-10-20")]
    [InlineData("2026-10-20", "2026-10-20", "26_10 (2026-10-01 - 2026-10-31)", "2026-10-20")]
    [InlineData("2026-10-20", "2026-10-21", "26_10 (2026-10-01 - 2026-10-31)", "2026-10-20")]
    // An entry of a closed period cannot be moved to an open one
    [InlineData("2026-10-31", "2026-11-02", "26_10 (2026-10-01 - 2026-10-31)", "2026-10-31")]
    // An entry of an open period cannot be moved to a closed one
    [InlineData("2026-11-03", "2026-10-31", "26_10 (2026-10-01 - 2026-10-31)", "2026-10-31")]
    [InlineData("2026-11-03", "2026-09-15", "26_09 (2026-09-01 - 2026-09-30)", "2026-09-15")]
    public static async Task UpdateAsync_PeriodOfCurrentOrNewDateIsClosedByTemporaryRule_ExpectPeriodClosedFailureAndTimesheetIsNotUpdated(
        string currentDate, string? newDate, string expectedPeriod, string expectedDate)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, BuildTimesheetDateJsonOut(ParseDate(currentDate)));
        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        var input = BuildUpdateInput(newDate is null ? null : ParseDate(newDate));
        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetUpdateFailureCode.PeriodClosed,
            $"The period {expectedPeriod} is closed: time entries of {expectedDate} cannot be created or changed.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("2026-11-03", null)]
    [InlineData("2026-11-03", "2026-11-01")]
    [InlineData("2026-11-01", "2026-11-28")]
    public static async Task UpdateAsync_PeriodsOfCurrentAndNewDatesAreOpenByTemporaryRule_ExpectDataverseUpdateCalledOnce(
        string currentDate, string? newDate)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, BuildTimesheetDateJsonOut(ParseDate(currentDate)));
        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        var input = BuildUpdateInput(newDate is null ? null : ParseDate(newDate));
        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_DateIsChanged_ExpectTemporaryRuleIsCheckedOnceForCurrentAndNewDates()
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, BuildTimesheetDateJsonOut(new(2026, 11, 03)));
        var mockOpenPeriodApi = BuildMockOpenPeriodApi();
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        _ = await api.UpdateAsync(BuildUpdateInput(new(2026, 11, 10)), TestContext.Current.CancellationToken);

        mockOpenPeriodApi.Verify(static a => a.FindClosedPeriodAsync(new(2026, 11, 03), It.IsAny<CancellationToken>()), Times.Once);
        mockOpenPeriodApi.Verify(static a => a.FindClosedPeriodAsync(new(2026, 11, 10), It.IsAny<CancellationToken>()), Times.Once);
        mockOpenPeriodApi.VerifyNoOtherCalls();
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyApiTest
{
    [Theory]
    [InlineData("2026-11-28")]
    [InlineData("2026-10-15")]
    public static async Task CreateAsync_InputDateIsNotInTheFuture_ExpectTemporaryRuleIsCheckedOnceForTheDate(string date)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var mockOpenPeriodApi = BuildMockOpenPeriodApi();
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        _ = await api.CreateAsync(BuildCreateInput(ParseDate(date)), TestContext.Current.CancellationToken);

        mockOpenPeriodApi.Verify(a => a.FindClosedPeriodAsync(ParseDate(date), It.IsAny<CancellationToken>()), Times.Once);
        mockOpenPeriodApi.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task CreateAsync_InputDateIsInTheFuture_ExpectTemporaryRuleIsNotChecked()
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var mockOpenPeriodApi = BuildMockOpenPeriodApi();
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        _ = await api.CreateAsync(BuildCreateInput(new(2026, 11, 29)), TestContext.Current.CancellationToken);

        mockOpenPeriodApi.VerifyNoOtherCalls();
    }

    // October 2026 is closed by the temporary rule, November is open
    [Theory]
    [InlineData("2026-10-31", "26_10 (2026-10-01 - 2026-10-31)")]
    [InlineData("2026-10-01", "26_10 (2026-10-01 - 2026-10-31)")]
    [InlineData("2026-09-30", "26_09 (2026-09-01 - 2026-09-30)")]
    [InlineData("2025-12-31", "25_12 (2025-12-01 - 2025-12-31)")]
    public static async Task CreateAsync_PeriodOfTheDateIsClosedByTemporaryRule_ExpectPeriodClosedFailureAndDataverseIsNotCalled(
        string date, string expectedPeriod)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.CreateAsync(BuildCreateInput(ParseDate(date)), TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetCreateFailureCode.PeriodClosed,
            $"The period {expectedPeriod} is closed: time entries of {date} cannot be created or changed.");

        Assert.StrictEqual(expected, actual);
        mockDataverseApi.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("2026-11-28")]
    [InlineData("2026-11-01")]
    public static async Task CreateAsync_PeriodOfTheDateIsOpenByTemporaryRule_ExpectDataverseCreateCalledOnce(string date)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var api = new TimesheetModifyApi(mockDataverseApi.Object, mockOpenPeriodApi.Object, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.CreateAsync(BuildCreateInput(ParseDate(date)), TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.CreateEntityAsync(It.IsAny<DataverseEntityCreateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

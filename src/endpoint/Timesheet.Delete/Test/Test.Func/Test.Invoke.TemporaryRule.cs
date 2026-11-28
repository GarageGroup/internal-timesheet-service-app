using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test;

partial class TimesheetDeleteFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectTemporaryRuleIsCheckedOnceForTimesheetDate()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            Result.Success<Unit>(default), BuildTimesheetJsonOut(new(2026, 10, 15)), NoPeriodJsonOut);

        var mockOpenPeriodApi = BuildMockOpenPeriodApi();
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, mockOpenPeriodApi.Object);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        mockOpenPeriodApi.Verify(static a => a.FindClosedPeriodAsync(new(2026, 10, 15), It.IsAny<CancellationToken>()), Times.Once);
        mockOpenPeriodApi.VerifyNoOtherCalls();
    }

    // October 2026 is closed by the temporary rule, November is open
    [Theory]
    [InlineData("2026-10-31", "26_10 (2026-10-01 - 2026-10-31)")]
    [InlineData("2026-10-01", "26_10 (2026-10-01 - 2026-10-31)")]
    [InlineData("2026-09-30", "26_09 (2026-09-01 - 2026-09-30)")]
    [InlineData("2025-12-31", "25_12 (2025-12-01 - 2025-12-31)")]
    public static async Task InvokeAsync_PeriodOfTimesheetDateIsClosedByTemporaryRule_ExpectPeriodClosedFailureAndTimesheetIsNotDeleted(
        string date, string expectedPeriod)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            Result.Success<Unit>(default), BuildTimesheetJsonOut(ParseDate(date)), NoPeriodJsonOut);

        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, mockOpenPeriodApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetDeleteFailureCode.PeriodClosed,
            $"The period {expectedPeriod} is closed: time entries of {date} cannot be deleted.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("2026-11-28")]
    [InlineData("2026-11-01")]
    [InlineData("2026-12-10")]
    public static async Task InvokeAsync_PeriodOfTimesheetDateIsOpenByTemporaryRule_ExpectTimesheetIsDeleted(string date)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            Result.Success<Unit>(default), BuildTimesheetJsonOut(ParseDate(date)), NoPeriodJsonOut);

        var mockOpenPeriodApi = BuildMockOpenPeriodApi(firstOpenDate: new(2026, 11, 01));
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, mockOpenPeriodApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DateOnly ParseDate(string date)
        =>
        DateOnly.ParseExact(date, "yyyy-MM-dd");
}

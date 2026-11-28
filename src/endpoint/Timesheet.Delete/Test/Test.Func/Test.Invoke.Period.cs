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
    public static async Task InvokeAsync_ExpectTimesheetDateIsRequestedOnceOnBehalfOfTheUser()
    {
        var mockDataverseApi = BuildMockDataverseDeleteApi(Result.Success<Unit>(default));
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntityGetIn(
            entityPluralName: "gg_timesheetactivities",
            entityKey: new DataversePrimaryKey(new("84de1ab3-e0a6-4666-a90c-4165c61522b8")),
            selectFields: ["gg_date"])
        {
            CallerObjectId = new("d290f1ee-6c54-4b01-90e6-d701748f0851")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<TimesheetJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.IsvAborted, TimesheetDeleteFailureCode.BadRequest)]
    public static async Task InvokeAsync_TimesheetGetResultIsFailure_ExpectFailureAndTimesheetIsNotDeleted(
        DataverseFailureCode sourceFailureCode, TimesheetDeleteFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default), dataverseFailure, NoPeriodJsonOut);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetIsNotFound_ExpectSuccessAndPeriodIsNotRequested()
    {
        var dataverseFailure = Failure.Create(DataverseFailureCode.RecordNotFound, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure, dataverseFailure, ClosedPeriodJsonOut);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(2024, 06, 05, "statecode eq 1 and gg_from_date le 2024-06-05 and gg_to_date ge 2024-06-05")]
    [InlineData(2026, 11, 28, "statecode eq 1 and gg_from_date le 2026-11-28 and gg_to_date ge 2026-11-28")]
    public static async Task InvokeAsync_ExpectClosedPeriodOfTimesheetDateIsRequestedOnce(
        int year, int month, int day, string expectedFilter)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            Result.Success<Unit>(default), BuildTimesheetJsonOut(new(year, month, day)), NoPeriodJsonOut);

        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_employee_cost_periods",
            selectFields: ["gg_name", "gg_from_date", "gg_to_date"],
            filter: expectedFilter,
            top: 1);

        mockDataverseApi.Verify(a => a.GetEntitySetAsync<PeriodJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetHasNoDate_ExpectPeriodIsNotRequestedAndTimesheetIsDeleted()
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default), BuildTimesheetJsonOut(null), ClosedPeriodJsonOut);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_PeriodOfTimesheetDateIsClosed_ExpectPeriodClosedFailureAndTimesheetIsNotDeleted()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            Result.Success<Unit>(default), BuildTimesheetJsonOut(new(2024, 05, 20)), ClosedPeriodJsonOut);

        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetDeleteFailureCode.PeriodClosed,
            "The period 24_05 (2024-05-01 - 2024-05-31) is closed: time entries of 2024-05-20 cannot be deleted.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.IsvAborted)]
    public static async Task InvokeAsync_PeriodGetResultIsFailure_ExpectUnknownFailureAndTimesheetIsNotDeleted(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default), SomeTimesheetJsonOut, dataverseFailure);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object, NoClosedPeriodApi);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetDeleteFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
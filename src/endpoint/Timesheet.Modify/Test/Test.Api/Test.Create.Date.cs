using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyApiTest
{
    private static TimesheetCreateIn BuildCreateInput(DateOnly date)
        =>
        new(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: date,
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

    [Theory]
    [InlineData(2026, 11, 29)]
    [InlineData(2026, 12, 01)]
    [InlineData(2027, 01, 15)]
    public static async Task CreateAsync_InputDateIsInTheFuture_ExpectFutureDateFailureAndDataverseIsNotCalled(
        int year, int month, int day)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.CreateAsync(BuildCreateInput(new(year, month, day)), TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetCreateFailureCode.FutureDate, $"Time cannot be logged for a future date: {year:0000}-{month:00}-{day:00}.");

        Assert.StrictEqual(expected, actual);
        mockDataverseApi.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(2026, 11, 28)]
    [InlineData(2026, 11, 27)]
    [InlineData(2025, 01, 01)]
    public static async Task CreateAsync_InputDateIsTodayOrEarlierAndPeriodIsNotClosed_ExpectDataverseCreateCalledOnce(
        int year, int month, int day)
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, BuildTodayProvider(new(2026, 11, 28)));

        var actual = await api.CreateAsync(BuildCreateInput(new(year, month, day)), TestContext.Current.CancellationToken);

        Assert.StrictEqual(Result.Success<Unit>(default), actual);

        mockDataverseApi.Verify(
            static a => a.CreateEntityAsync(It.IsAny<DataverseEntityCreateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync_InputDateIsNotInTheFuture_ExpectClosedPeriodOfTheDateIsRequestedOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(NoPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        _ = await api.CreateAsync(BuildCreateInput(new(2024, 05, 17)), TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_employee_cost_periods",
            selectFields: ["gg_name", "gg_from_date", "gg_to_date"],
            filter: "statecode eq 1 and ((gg_from_date le 2024-05-17 and gg_to_date ge 2024-05-17))",
            top: 1);

        mockDataverseApi.Verify(a => a.GetEntitySetAsync<PeriodJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync_PeriodOfTheDateIsClosed_ExpectPeriodClosedFailureAndTimesheetIsNotCreated()
    {
        var mockDataverseApi = BuildMockDataverseApi(ClosedPeriodJsonOut, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var actual = await api.CreateAsync(BuildCreateInput(new(2024, 05, 17)), TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            TimesheetCreateFailureCode.PeriodClosed,
            "The period 24_05 (2024-05-01 - 2024-05-31) is closed: time entries of 2024-05-17 cannot be created or changed.");

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.CreateEntityAsync(It.IsAny<DataverseEntityCreateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);

        mockDataverseApi.Verify(
            static a => a.GetEntityAsync<ProjectJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    public static async Task CreateAsync_PeriodGetResultIsFailure_ExpectUnknownFailureAndTimesheetIsNotCreated(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure, SomeTimesheetDateJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object, NoClosedPeriodApi, SomeTodayProvider);

        var actual = await api.CreateAsync(BuildCreateInput(new(2024, 05, 17)), TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetCreateFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);

        mockDataverseApi.Verify(
            static a => a.CreateEntityAsync(It.IsAny<DataverseEntityCreateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

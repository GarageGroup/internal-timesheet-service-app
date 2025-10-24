using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Cost.Endpoint.CostPeriod.GetSet.Test;

partial class PeriodSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectDataverseSetGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomePeriodJsonOut);
        var mockTodayProvider = BuildTodayProvider(new(2025, 11, 12, 12, 0, 0));
        var func = new PeriodSetGetFunc(mockDataverseApi.Object, mockTodayProvider);

        _ = await func.InvokeAsync(default, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_employee_cost_periods",
            selectFields: ["gg_name", "gg_from_date", "gg_to_date"],
            filter: "statecode eq 0 and gg_from_date lt 2025-11-12T12:00:00.0000000",
            orderBy:
            [
                new("gg_to_date", DataverseOrderDirection.Descending)
            ]);

        mockDataverseApi.Verify(a => a.GetEntitySetAsync<PeriodJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task InvokeAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new PeriodSetGetFunc(mockDataverseApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(default, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(PeriodSetGetFuncSource.OutputTestData), MemberType = typeof(PeriodSetGetFuncSource))]
    internal static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess(
        DataverseEntitySetGetOut<PeriodJson> dataverseOut, PeriodSetGetOut expected)
    {
        var mockDataverseApi = BuildMockDataverseApi(dataverseOut);
        var mockTodayProvider = BuildTodayProvider(SomeToday);
        var func = new PeriodSetGetFunc(mockDataverseApi.Object, mockTodayProvider);

        var actual = await func.InvokeAsync(default, TestContext.Current.CancellationToken);

        Assert.StrictEqual(expected, actual);
    }
}
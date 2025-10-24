using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test;

partial class TimesheetDeleteFuncTest
{
    [Theory]
    [MemberData(nameof(TimesheetDeleteFuncSource.InputTestData), MemberType = typeof(TimesheetDeleteFuncSource))]
    public static async Task InvokeAsync_ExpectDataverseDeleteCalledOnce(
        TimesheetDeleteIn input, DataverseEntityDeleteIn expectedInput)
    {
        var mockDataverseApi = BuildMockDataverseDeleteApi(Result.Success<Unit>(default));
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object);

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(a => a.DeleteEntityAsync(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.CannotUpdateBecauseItIsReadOnly, TimesheetDeleteFailureCode.BadRequest)]
    [InlineData(DataverseFailureCode.IsvAborted, TimesheetDeleteFailureCode.BadRequest)]
    public static async Task InvokeAsync_DataverseResultIsUnexpectedFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetDeleteFailureCode expectFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseDeleteApi(dataverseFailure);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsFailureRecordNotFoundFailure_ExpectSuccess()
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(DataverseFailureCode.RecordNotFound, "Some failure message");

        var mockDataverseApi = BuildMockDataverseDeleteApi(dataverseFailure);
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseDeleteApi(Result.Success<Unit>(default));
        var func = new TimesheetDeleteFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
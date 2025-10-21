using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SearchSet.Test;

partial class ProjectSetSearchFuncTest
{
    [Theory]
    [MemberData(nameof(ProjectSetSearchFuncSource.InputTestData), MemberType = typeof(ProjectSetSearchFuncSource))]
    public static async Task InvokeAsync_ExpectDataverseSearchCalledOnce(
        ProjectSetSearchIn input, DataverseSearchIn expectedInput)
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeDataverseOutput);
        var func = new ProjectSetSearchFunc(mockDataverseApi.Object);

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(a => a.SearchAsync(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, ProjectSetSearchFailureCode.Forbidden)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, ProjectSetSearchFailureCode.Forbidden)]
    [InlineData(DataverseFailureCode.Throttling, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, ProjectSetSearchFailureCode.Forbidden)]
    [InlineData(DataverseFailureCode.DuplicateRecord, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, ProjectSetSearchFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, ProjectSetSearchFailureCode.Unknown)]
    public static async Task SearchProjectSetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, ProjectSetSearchFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var func = new ProjectSetSearchFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(SomeSearchInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ProjectSetSearchFuncSource.OutputTestData), MemberType = typeof(ProjectSetSearchFuncSource))]
    public static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess(
        DataverseSearchOut dataverseOutput, ProjectSetSearchOut expected)
    {
        var mockDataverseApi = BuildMockDataverseApi(dataverseOutput);

        var func = new ProjectSetSearchFunc(mockDataverseApi.Object);
        var actual = await func.InvokeAsync(SomeSearchInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(expected, actual);
    }
}
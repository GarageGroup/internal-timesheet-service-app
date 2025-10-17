using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyFuncTest
{
    [Fact]
    public static async Task InvokeAsync_Update_InputProjectTypeIsProject_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        _ = await func.InvokeAsync(input, cancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "gg_projects",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["gg_name"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<ProjectJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Update_InputProjectTypeIsIncident_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Incident),
            duration: 2,
            description: "Some description");

        _ = await func.InvokeAsync(input, cancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "incidents",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["title"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<IncidentJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Update_InputProjectTypeIsOpportunity_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Opportunity),
            duration: 2,
            description: "Some description");

        _ = await func.InvokeAsync(input, cancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "opportunities",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["name"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<OpportunityJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Update_InputProjectTypeIsLead_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), SomeLeadJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Lead),
            duration: 2,
            description: "Some description");

        _ = await func.InvokeAsync(input, cancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "leads",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["companyname", "subject"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<LeadJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Update_InputProjectTypeIsInvalid_ExpectUnexpectedProjectTypeFailure()
    {
        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: (ProjectType)(-5)),
            duration: 2,
            description: "Some description");

        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);

        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(TimesheetUpdateFailureCode.UnexpectedProjectType, "An unexpected project type: -5");

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetUpdateFailureCode.ProjectNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, TimesheetUpdateFailureCode.Unknown)]
    public static async Task InvokeAsync_Update_ProjectGetResultIsFailure_ExpectFailure(
        DataverseFailureCode dataverseFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(dataverseFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseFailure);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputUpdateProjectTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Update_ProjectGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<ProjectJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputUpdateIncidentTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Update_IncidentGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<IncidentJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputUpdateOpportunityTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Update_OpportunityGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<OpportunityJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputUpdateLeadTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Update_LeadGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<LeadJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetUpdateFailureCode.TimesheetNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, TimesheetUpdateFailureCode.Unknown)]
    public static async Task InvokeAsync_Update_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(dataverseFailure, SomeProjectJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_Update_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Incident),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}

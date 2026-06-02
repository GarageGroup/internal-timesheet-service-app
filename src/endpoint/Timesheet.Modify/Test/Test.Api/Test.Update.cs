using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyApiTest
{
    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsProject_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "gg_projects",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["gg_name"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<ProjectJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsIncident_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Incident),
            duration: 2,
            description: "Some description");

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "incidents",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["title"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<IncidentJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsOpportunity_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Opportunity),
            duration: 2,
            description: "Some description");

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "opportunities",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["name"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<OpportunityJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsLead_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), SomeLeadJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Lead),
            duration: 2,
            description: "Some description");

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "leads",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["companyname", "subject"])
        {
            CallerObjectId = new("f4597142-5400-4776-8d71-8b50842978af")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<LeadJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsInvalid_ExpectUnexpectedProjectTypeFailure()
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

        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);
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
    public static async Task UpdateAsync_ProjectGetResultIsFailure_ExpectFailure(
        DataverseFailureCode dataverseFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(dataverseFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseFailure);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputUpdateProjectTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task UpdateAsync_ProjectGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<ProjectJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(
                It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputUpdateIncidentTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task UpdateAsync_IncidentGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<IncidentJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(
                It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputUpdateOpportunityTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task UpdateAsync_OpportunityGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<OpportunityJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(
                It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputUpdateLeadTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task UpdateAsync_LeadGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityGetOut<LeadJson> dataverseOut, DataverseEntityUpdateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.UpdateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.UpdateEntityAsync(
                It.Is<DataverseEntityUpdateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
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
    [InlineData(DataverseFailureCode.CannotUpdateBecauseItIsReadOnly, TimesheetUpdateFailureCode.BadRequest)]
    [InlineData(DataverseFailureCode.IsvAborted, TimesheetUpdateFailureCode.BadRequest)]
    public static async Task UpdateAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(dataverseFailure, SomeProjectJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task UpdateAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetUpdateIn(
            systemUserId: new("f4597142-5400-4776-8d71-8b50842978af"),
            timesheetId: new("80108b86-61ae-47ea-bd61-6d0c126a42b4"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Incident),
            duration: 2,
            description: "Some description");

        var actual = await api.UpdateAsync(input, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
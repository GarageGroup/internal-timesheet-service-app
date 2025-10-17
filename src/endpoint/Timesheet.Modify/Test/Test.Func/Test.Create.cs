using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

partial class TimesheetModifyFuncTest 
{
    [Theory]
    [InlineData("")]
    [InlineData("\n\r")]
    [InlineData(null)]
    internal static async Task InvokeAsync_Create_InputDescriptionIsEmpty_ExpectFailure(string? description)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: description);

        var actual = await func.InvokeAsync(input, cancellationToken);
        var expected = Failure.Create(TimesheetCreateFailureCode.EmptyDescription, "Description is empty");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputProjectTypeIsProject_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
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
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<ProjectJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputProjectTypeIsIncident_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
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
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<IncidentJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputProjectTypeIsOpportunity_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
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
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<OpportunityJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputProjectTypeIsLead_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), SomeLeadJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
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
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<LeadJson>(expectInput, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputProjectTypeIsInvalid_ExpectUnexpectedProjectTypeFailureCode()
    {
        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: (ProjectType)(-5)),
            duration: 2,
            description: "Some description");

        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);

        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(TimesheetCreateFailureCode.UnexpectedProjectType, "An unexpected project type: -5");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_Create_InputIsValidProjectIdIsInvalid_ExpectProjectNotFoundFailureCode()
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(DataverseFailureCode.RecordNotFound, "Some failure text");
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseFailure);

        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(TimesheetCreateFailureCode.ProjectNotFound, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputCreateProjectTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Create_InputIsValidForProject_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<ProjectJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputCreateIncidentTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Create_InputIsValidForIncident_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<IncidentJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputCreateOpportunityTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Create_InputIsValidForOpportunity_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<OpportunityJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyFuncSource.InputCreateLeadTestData), MemberType = typeof(TimesheetModifyFuncSource))]
    internal static async Task InvokeAsync_Create_InputIsValidForLead_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<LeadJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), dataverseOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetCreateFailureCode.Forbidden)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetCreateFailureCode.Forbidden)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, TimesheetCreateFailureCode.Unknown)]
    public static async Task InvokeAsync_Create_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetCreateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(dataverseFailure, SomeLeadJsonOut);

        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Lead),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_Create_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var func = new TimesheetModifyFunc(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Opportunity),
            duration: 2,
            description: "Some description");

        var actual = await func.InvokeAsync(input, default);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
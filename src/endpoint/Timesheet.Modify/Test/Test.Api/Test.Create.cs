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
    [InlineData("")]
    [InlineData("\n\r")]
    [InlineData(null)]
    internal static async Task CreateAsync_InputDescriptionIsEmpty_ExpectFailure(string? description)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: description);

        var actual = await api.CreateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetCreateFailureCode.EmptyDescription, "Description is empty");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync__InputProjectTypeIsProject_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "gg_projects",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["gg_name"])
        {
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<ProjectJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync__InputProjectTypeIsIncident_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), SomeIncidentJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Incident),
            duration: 2,
            description: "Some description");

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "incidents",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["title"])
        {
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<IncidentJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync__InputProjectTypeIsOpportunity_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Opportunity),
            duration: 2,
            description: "Some description");

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "opportunities",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["name"])
        {
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<OpportunityJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync__InputProjectTypeIsLead_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), SomeLeadJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Lead),
            duration: 2,
            description: "Some description");

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        var expectInput = new DataverseEntityGetIn(
            entityPluralName: "leads",
            entityKey: new DataversePrimaryKey(new("190fd90c-64be-4d6e-8764-44c567b40ef9")),
            selectFields: ["companyname", "subject"])
        {
            CallerObjectId = new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766")
        };

        mockDataverseApi.Verify(a => a.GetEntityAsync<LeadJson>(expectInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task CreateAsync__InputProjectTypeIsInvalid_ExpectUnexpectedProjectTypeFailureCode()
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

        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var actual = await api.CreateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetCreateFailureCode.UnexpectedProjectType, "An unexpected project type: -5");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync__InputIsValidProjectIdIsInvalid_ExpectProjectNotFoundFailureCode()
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(DataverseFailureCode.RecordNotFound, "Some failure text");
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseFailure);

        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Project),
            duration: 2,
            description: "Some description");

        var actual = await api.CreateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(TimesheetCreateFailureCode.ProjectNotFound, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputCreateProjectTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task CreateAsync__InputIsValidForProject_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<ProjectJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(
                It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputCreateIncidentTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task CreateAsync__InputIsValidForIncident_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<IncidentJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<IncidentJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(
                It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputCreateOpportunityTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task CreateAsync__InputIsValidForOpportunity_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<OpportunityJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(
                It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(TimesheetModifyApiSource.InputCreateLeadTestData), MemberType = typeof(TimesheetModifyApiSource))]
    internal static async Task CreateAsync__InputIsValidForLead_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityGetOut<LeadJson> dataverseOut, DataverseEntityCreateIn<TimesheetJson> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(Result.Success<Unit>(default), dataverseOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        _ = await api.CreateAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(
            a => a.CreateEntityAsync(
                It.Is<DataverseEntityCreateIn<TimesheetJson>>(@in => AreEqual(expected, @in)), It.IsAny<CancellationToken>()),
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
    [InlineData(DataverseFailureCode.CannotUpdateBecauseItIsReadOnly, TimesheetCreateFailureCode.BadRequest)]
    [InlineData(DataverseFailureCode.IsvAborted, TimesheetCreateFailureCode.BadRequest)]
    public static async Task CreateAsync__DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetCreateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi<LeadJson>(dataverseFailure, SomeLeadJsonOut);

        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Lead),
            duration: 2,
            description: "Some description");

        var actual = await api.CreateAsync(input, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync__DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi<OpportunityJson>(Result.Success<Unit>(default), SomeOpportunityJsonOut);
        var api = new TimesheetModifyApi(mockDataverseApi.Object);

        var input = new TimesheetCreateIn(
            systemUserId: new("a3fc6a92-4e7c-4fea-a8c5-3aa432a4e766"),
            date: new(2024, 06, 07),
            project: new(
                id: new("190fd90c-64be-4d6e-8764-44c567b40ef9"),
                type: ProjectType.Opportunity),
            duration: 2,
            description: "Some description");

        var actual = await api.CreateAsync(input, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
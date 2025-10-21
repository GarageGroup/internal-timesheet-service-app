using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SetGet.Test;

public static partial class ProjectSetGetFuncTest
{
    private static readonly ProjectSetGetIn SomeInput
        =
        new(
            systemUserId: new("2e8c5f2c-b93e-4b95-a115-459cf9df6de6"));

    private static readonly FlatArray<DbIncident> SomeDbIncidentOutput
        =
        [
            new()
            {
                ProjectId = new("63d9e1b7-706b-ea11-a813-000d3a44ad35"),
                ProjectName = "Some first incident name",
            },
            new()
            {
                ProjectId = new("20f2d7f3-c73d-4895-aa09-c8cdb3cd0acd"),
                ProjectName = "Some second incident name"
            }
        ];

    private static readonly FlatArray<DbProject> SomeDbProjectOutput
        =
        [
            new()
            {
                ProjectId = new("3b8757b4-8940-43cd-8892-c2415254c7ac"),
                ProjectName = "Some first project name",
            },
            new()
            {
                ProjectId = new("7aaefe05-ca78-43bb-8a8e-a5aa25aca293"),
                ProjectName = "Some second project name"
            }
        ];

    private static readonly FlatArray<DbOpportunity> SomeDbOpportunityOutput
        =
        [
            new()
            {
                ProjectId = new("fadada98-931b-4587-b482-6b8b53a672b6"),
                ProjectName = "Some first opportunity name",
            },
            new()
            {
                ProjectId = new("a4e19a22-aa70-4dc3-94e0-0344a492b935"),
                ProjectName = "Some second opportunity name"
            }
        ];

    private static readonly FlatArray<DbLead> SomeDbLeadOutput
        =
        [
            new()
            {
                ProjectId = new("b78e1239-e85e-4cac-b89a-20b92282de7f"),
                CompanyName = "Some first company name",
                Subject = "Some first subject"
            },
            new()
            {
                ProjectId = new("113bd05f-83f8-4542-9bd8-884b719a3f57"),
                CompanyName = "Some second company name",
                Subject = "Some second subject"
            }
        ];

    private static readonly DateOnly SomeToday
        =
        new(2024, 05, 30);

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi(
        in Result<FlatArray<DbIncident>, Failure<Unit>> incidentResult,
        in Result<FlatArray<DbProject>, Failure<Unit>> projectResult,
        in Result<FlatArray<DbOpportunity>, Failure<Unit>> opportunityResult,
        in Result<FlatArray<DbLead>, Failure<Unit>> leadResult)
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbIncident>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidentResult);

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbProject>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectResult);

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbOpportunity>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunityResult);

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbLead>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(leadResult);

        return mock;
    }

    private static ITodayProvider BuildTodayProvider(DateOnly date)
        =>
        Mock.Of<ITodayProvider>(t => t.Today == date);
}
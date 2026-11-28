using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Modify.Test;

public static partial class TimesheetModifyApiTest
{
    private static readonly DataverseEntityGetOut<ProjectJson> SomeProjectJsonOut
        =
        new(
            value: new()
            {
                Id = new("7babe661-cc69-474a-95a5-82ce45204ff8"),
                ProjectName = "Some project name"
            });

    private static readonly DataverseEntityGetOut<IncidentJson> SomeIncidentJsonOut
        =
        new(
            value: new()
            {
                Id = new("7babe661-cc69-474a-95a5-82ce45204ff8"),
                ProjectName = "Some project name"
            });

    private static readonly DataverseEntityGetOut<OpportunityJson> SomeOpportunityJsonOut
        =
        new(
            value: new()
            {
                Id = new("7babe661-cc69-474a-95a5-82ce45204ff8"),
                ProjectName = "Some project name"
            });

    private static readonly DataverseEntityGetOut<LeadJson> SomeLeadJsonOut
        =
        new(
            value: new()
            {
                Id = new("7babe661-cc69-474a-95a5-82ce45204ff8"),
                CompanyName = "Some company",
                Subject = "Some subject"
            });

    // The existing tests use dates of 2024, so they are in the past for this provider
    private static readonly ITodayProvider SomeTodayProvider
        =
        BuildTodayProvider(new(2026, 11, 28));

    private static readonly DataverseEntityGetOut<TimesheetDateJson> SomeTimesheetDateJsonOut
        =
        new(
            value: new()
            {
                Date = new(2024, 06, 05)
            });

    private static readonly DataverseEntitySetGetOut<PeriodJson> NoPeriodJsonOut
        =
        new(value: default);

    private static readonly DataverseEntitySetGetOut<PeriodJson> ClosedPeriodJsonOut
        =
        new(
            value:
            [
                new()
                {
                    Name = "24_05",
                    From = new(2024, 05, 01),
                    To = new(2024, 05, 31)
                }
            ]);

    // The temporary rule closes no period
    private static readonly IOpenPeriodApi NoClosedPeriodApi
        =
        BuildMockOpenPeriodApi().Object;

    // The temporary rule closes every period before the first open date
    private static Mock<IOpenPeriodApi> BuildMockOpenPeriodApi(DateOnly? firstOpenDate = null)
    {
        var mock = new Mock<IOpenPeriodApi>();

        _ = mock
            .Setup(static a => a.FindClosedPeriodAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (DateOnly date, CancellationToken _) => date < firstOpenDate
                    ? new ClosedPeriod(
                        Name: date.ToString("yy_MM"),
                        From: new(date.Year, date.Month, 1),
                        To: new DateOnly(date.Year, date.Month, 1).AddMonths(1).AddDays(-1))
                    : null);

        return mock;
    }

    private static DateOnly ParseDate(string date)
        =>
        DateOnly.ParseExact(date, "yyyy-MM-dd");

    private static ITodayProvider BuildTodayProvider(DateOnly today)
        =>
        Mock.Of<ITodayProvider>(t => t.Today == today);

    // The mock of an API where the project exists and the dates may be checked
    private static Mock<IDataverseApiClient> BuildMockDataverseApi(
        in Result<DataverseEntitySetGetOut<PeriodJson>, Failure<DataverseFailureCode>> periodResult,
        in Result<DataverseEntityGetOut<TimesheetDateJson>, Failure<DataverseFailureCode>> timesheetDateResult)
    {
        var mock = BuildMockDataverseApi<ProjectJson>(Result.Success<Unit>(default), SomeProjectJsonOut);

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodResult);

        _ = mock
            .Setup(static a => a.GetEntityAsync<TimesheetDateJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheetDateResult);

        return mock;
    }

    private static Mock<IDataverseApiClient> BuildMockDataverseApi<TOut>(
        in Result<Unit, Failure<DataverseFailureCode>> result,
        in Result<DataverseEntityGetOut<TOut>, Failure<DataverseFailureCode>> projectNameResult)
        where TOut : IProjectJson, IProjectDataverseInputBuilder
    {
        var mock = new Mock<IDataverseApiClient>();

        _ = mock
            .Setup(static a => a.CreateEntityAsync(It.IsAny<DataverseEntityCreateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock
            .Setup(static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<TimesheetJson>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock
            .Setup(static a => a.GetEntityAsync<TOut>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectNameResult);

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NoPeriodJsonOut);

        _ = mock
            .Setup(static a => a.GetEntityAsync<TimesheetDateJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SomeTimesheetDateJsonOut);

        return mock;
    }

    private static bool AreEqual(DataverseEntityCreateIn<TimesheetJson> expected, DataverseEntityCreateIn<TimesheetJson> actual)
    {
        Assert.Equivalent(expected, actual);
        return true;
    }

    private static bool AreEqual(DataverseEntityUpdateIn<TimesheetJson> expected, DataverseEntityUpdateIn<TimesheetJson> actual)
    {
        Assert.Equivalent(expected, actual);
        return true;
    }
}
using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.Delete.Test;

public static partial class TimesheetDeleteFuncTest
{
    private static readonly TimesheetDeleteIn SomeInput
        =
        new(
            systemUserId: new("d290f1ee-6c54-4b01-90e6-d701748f0851"),
            timesheetId: new("84de1ab3-e0a6-4666-a90c-4165c61522b8"));

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

    private static readonly DataverseEntityGetOut<TimesheetJson> SomeTimesheetJsonOut
        =
        BuildTimesheetJsonOut(new(2024, 06, 05));

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

    private static DataverseEntityGetOut<TimesheetJson> BuildTimesheetJsonOut(DateOnly? date)
        =>
        new(
            value: new()
            {
                Date = date
            });

    // The mock of an API where the entry exists and its period is not closed
    private static Mock<IDataverseApiClient> BuildMockDataverseDeleteApi(
        in Result<Unit, Failure<DataverseFailureCode>> result)
        =>
        BuildMockDataverseApi(result, SomeTimesheetJsonOut, NoPeriodJsonOut);

    private static Mock<IDataverseApiClient> BuildMockDataverseApi(
        in Result<Unit, Failure<DataverseFailureCode>> deleteResult,
        in Result<DataverseEntityGetOut<TimesheetJson>, Failure<DataverseFailureCode>> timesheetResult,
        in Result<DataverseEntitySetGetOut<PeriodJson>, Failure<DataverseFailureCode>> periodResult)
    {
        var mock = new Mock<IDataverseApiClient>();

        _ = mock
            .Setup(static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult);

        _ = mock
            .Setup(static a => a.GetEntityAsync<TimesheetJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheetResult);

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodResult);

        return mock;
    }
}
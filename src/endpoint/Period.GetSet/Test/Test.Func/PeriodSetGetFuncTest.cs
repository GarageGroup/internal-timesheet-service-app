using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;

namespace GarageGroup.Internal.Timesheet.Cost.Endpoint.CostPeriod.GetSet.Test;

public static partial class PeriodSetGetFuncTest
{
    private static readonly DataverseEntitySetGetOut<PeriodJson> SomePeriodJsonOut
        =
        new(
            value:
            [
                new()
                {
                    Name = "Some first name",
                    From = new(2024, 6, 1, 21, 22, 3),
                    To = new(2024, 6, 30, 21, 22, 3)
                },
                new()
                {
                    Name = "Some second name",
                    From = new(2024, 5, 10, 21, 22, 3),
                    To = new(2024, 5, 15, 21, 22, 3)
                }
            ]);

    private static readonly DateTime SomeToday
        =
        new(2024, 05, 30, 12, 0, 0);

    private static Mock<IDataverseEntitySetGetSupplier> BuildMockDataverseApi(
        in Result<DataverseEntitySetGetOut<PeriodJson>, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IDataverseEntitySetGetSupplier>();

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<PeriodJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static ITodayProvider BuildTodayProvider(DateTime dateTime)
        =>
        Mock.Of<ITodayProvider>(t => t.Today == dateTime);
}
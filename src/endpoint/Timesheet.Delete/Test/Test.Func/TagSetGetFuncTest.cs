using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Tag.GetSet.Test;

public static partial class TagSetGetFuncTest
{
    private static readonly TagSetGetOption SomeOption
        =
        new(
            daysPeriod: 30);

    private static readonly DateOnly SomeDate
        =
        new(2023, 07, 24);

    private static readonly FlatArray<DbTag> SomeDbTimesheetTagSet
        =
        [
            new()
            {
                Description = "#TaskOne. Some text"
            },
            new()
            {
                Description = "#TaskTwo. More text"
            }
        ];

    private static readonly TagSetGetIn SomeTimesheetTagSetGetInput
        =
        new(
            systemUserId: new("54f0d2cf-93a3-417e-a21a-bff4e16c1b25"),
            projectId: new("6f8f07d6-b7e4-4b00-a829-e680c0375d1e"));

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi(
        in Result<FlatArray<DbTag>, Failure<Unit>> result)
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbTag>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static ITodayProvider BuildTodayProvider(DateOnly date)
        =>
        Mock.Of<ITodayProvider>(t => t.Today == date);
}
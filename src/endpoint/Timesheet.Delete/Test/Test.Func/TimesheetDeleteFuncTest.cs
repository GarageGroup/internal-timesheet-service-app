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

    private static Mock<IDataverseEntityDeleteSupplier> BuildMockDataverseDeleteApi(
        in Result<Unit, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IDataverseEntityDeleteSupplier>();

        _ = mock
            .Setup(static a => a.DeleteEntityAsync(It.IsAny<DataverseEntityDeleteIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}
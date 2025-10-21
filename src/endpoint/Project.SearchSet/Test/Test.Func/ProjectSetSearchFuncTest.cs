using System;
using System.Text.Json;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SearchSet.Test;

public static partial class ProjectSetSearchFuncTest
{
    private static readonly ProjectSetSearchIn SomeSearchInput
        =
        new(
            systemUserId: new("45b6e085-4d6e-4b2d-a26c-eb8c1c5a2e5e"),
            searchText: "Some search project name",
            top: 10);

    private static readonly DataverseSearchOut SomeDataverseOutput
        =
        new(
            totalRecordCount: 3,
            value:
            [
                new(
                    searchScore: 18.698789596557617,
                    objectId: new("cc1efd36-ceca-eb11-bacc-000d3a47050c"),
                    entityName: "opportunity",
                    extensionData: default),
                new(
                    searchScore: 19128,
                    objectId: new("727621b9-e663-44c1-a879-11b53596be4d"),
                    entityName: "lead",
                    extensionData:
                    [
                        new("subject", new(JsonSerializer.SerializeToElement("Some subject"))),
                        new("companyname", new(JsonSerializer.SerializeToElement("Some company name")))
                    ])
            ]);

    private static Mock<IDataverseSearchSupplier> BuildMockDataverseApi(
        in Result<DataverseSearchOut, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IDataverseSearchSupplier>();

        _ = mock
            .Setup(static a => a.SearchAsync(It.IsAny<DataverseSearchIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}
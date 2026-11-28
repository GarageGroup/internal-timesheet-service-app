using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.SetGet.Test;

partial class ProjectSetGetFuncTest
{
    // The source contains eight items: two of each type
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(8, 8)]
    [InlineData(100, 8)]
    public static async Task InvokeAsync_InputTopIsPositive_ExpectFirstItemsOfTheFullList(int top, int expectedCount)
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, BuildTodayProvider(new(2024, 05, 30)));

        var full = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var actual = await func.InvokeAsync(new(SomeInput.SystemUserId, top), TestContext.Current.CancellationToken);

        var fullProjects = full.SuccessOrThrow().Projects.AsEnumerable().ToArray();
        var actualProjects = actual.SuccessOrThrow().Projects.AsEnumerable().ToArray();

        Assert.Equal(8, fullProjects.Length);
        Assert.Equal(expectedCount, actualProjects.Length);
        Assert.Equal(fullProjects.Take(expectedCount), actualProjects);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public static async Task InvokeAsync_InputTopIsAbsentOrNotPositive_ExpectAllItems(int? top)
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbIncidentOutput, SomeDbProjectOutput, SomeDbOpportunityOutput, SomeDbLeadOutput);
        var func = new ProjectSetGetFunc(mockSqlApi.Object, BuildTodayProvider(new(2024, 05, 30)));

        var actual = await func.InvokeAsync(new(SomeInput.SystemUserId, top), TestContext.Current.CancellationToken);

        Assert.Equal(8, actual.SuccessOrThrow().Projects.Length);
    }
}

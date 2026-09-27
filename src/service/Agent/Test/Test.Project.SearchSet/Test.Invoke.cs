using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentProjectSetSearchFuncTest
{
    [Fact]
    public static async Task InvokeAsync_SearchTextIsEmpty_ExpectInvalidSearchTextFailure()
    {
        var mockProjectSearchFunc = BuildMockProjectSearchFunc(default(ProjectSetSearchOut));
        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(SomeContext, new("  ", 5), TestContext.Current.CancellationToken);

        Assert.Equal(AgentProjectSetSearchFailureCode.InvalidSearchText, actual.FailureOrThrow().FailureCode);
        mockProjectSearchFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_SearchTextIsTooLong_ExpectSearchTextTooLongFailure()
    {
        var mockProjectSearchFunc = BuildMockProjectSearchFunc(default(ProjectSetSearchOut));
        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(SomeContext, new(new('a', 101), 5), TestContext.Current.CancellationToken);

        Assert.Equal(AgentProjectSetSearchFailureCode.SearchTextTooLong, actual.FailureOrThrow().FailureCode);
        mockProjectSearchFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public static async Task InvokeAsync_TopIsInvalid_ExpectInvalidTopFailure(int top)
    {
        var mockProjectSearchFunc = BuildMockProjectSearchFunc(default(ProjectSetSearchOut));
        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(SomeContext, new("Project", top), TestContext.Current.CancellationToken);

        Assert.Equal(AgentProjectSetSearchFailureCode.InvalidTop, actual.FailureOrThrow().FailureCode);
        mockProjectSearchFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ValidInput_ExpectTrustedEntraObjectIdAndNormalizedInput()
    {
        var mockProjectSearchFunc = BuildMockProjectSearchFunc(default(ProjectSetSearchOut));
        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);

        _ = await func.InvokeAsync(SomeContext, new("  Project  ", null), TestContext.Current.CancellationToken);

        var expected = new ProjectSetSearchIn(SomeContext.EntraObjectId, "Project", SomeOption.DefaultTop);
        mockProjectSearchFunc.Verify(f => f.InvokeAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ProjectSetSearchFailureCode.Unknown, AgentProjectSetSearchFailureCode.Unknown)]
    [InlineData(ProjectSetSearchFailureCode.Forbidden, AgentProjectSetSearchFailureCode.Forbidden)]
    public static async Task InvokeAsync_ProjectSearchResultIsFailure_ExpectMappedFailure(
        ProjectSetSearchFailureCode sourceCode,
        AgentProjectSetSearchFailureCode expectedCode)
    {
        var sourceException = new Exception("Some error");
        var mockProjectSearchFunc = BuildMockProjectSearchFunc(
            sourceException.ToFailure(sourceCode, "Failed to search projects"));

        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);
        var actual = await func.InvokeAsync(
            SomeContext,
            new("Project", 5),
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(expectedCode, failure.FailureCode);
        Assert.Same(sourceException, failure.SourceException);
    }

    [Fact]
    public static async Task InvokeAsync_ProjectSearchResultIsSuccess_ExpectMappedProjects()
    {
        var project = new ProjectItem(
            id: new("f5f6c10c-bb43-4bdf-ae42-cbff244bd175"),
            name: "Some project",
            type: ProjectType.Project);

        var mockProjectSearchFunc = BuildMockProjectSearchFunc(
            new ProjectSetSearchOut
            {
                Projects = [project]
            });

        var func = new AgentProjectSetSearchFunc(mockProjectSearchFunc.Object, SomeOption);
        var actual = await func.InvokeAsync(
            SomeContext,
            new("Project", 5),
            TestContext.Current.CancellationToken);

        var expected = new AgentProjectSetSearchOut
        {
            Projects = [new(project.Id, project.Name, project.Type)]
        };

        Assert.StrictEqual(expected, actual.SuccessOrThrow());
    }
}

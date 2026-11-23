extern alias LastProjectContract;

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

using LastProjectItem = LastProjectContract::GarageGroup.Internal.Timesheet.ProjectItem;

partial class AgentLastProjectSetGetFuncTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public static async Task InvokeAsync_TopIsInvalid_ExpectInvalidTopFailure(int top)
    {
        var mockLastProjectFunc = BuildMockLastProjectFunc(default(LastProjectSetGetOut));
        var func = new AgentLastProjectSetGetFunc(mockLastProjectFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(SomeContext, new(top), TestContext.Current.CancellationToken);

        Assert.Equal(AgentLastProjectSetGetFailureCode.InvalidTop, actual.FailureOrThrow().FailureCode);
        mockLastProjectFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ValidInput_ExpectTrustedEntraObjectIdAndDefaultTop()
    {
        var mockLastProjectFunc = BuildMockLastProjectFunc(default(LastProjectSetGetOut));
        var func = new AgentLastProjectSetGetFunc(mockLastProjectFunc.Object, SomeOption);

        _ = await func.InvokeAsync(SomeContext, new(null), TestContext.Current.CancellationToken);

        var expected = new LastProjectSetGetIn(SomeContext.EntraObjectId, SomeOption.DefaultTop);
        mockLastProjectFunc.Verify(f => f.InvokeAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_LastProjectResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error");
        var mockLastProjectFunc = BuildMockLastProjectFunc(sourceException.ToFailure("Failed to get recent projects"));
        var func = new AgentLastProjectSetGetFunc(mockLastProjectFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(SomeContext, new(5), TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            AgentLastProjectSetGetFailureCode.Unknown,
            "Failed to get recent projects",
            sourceException);

        Assert.StrictEqual(expected, actual.FailureOrThrow());
    }

    [Fact]
    public static async Task InvokeAsync_LastProjectResultIsSuccess_ExpectMappedProjects()
    {
        var project = new LastProjectItem(
            id: new("fe91e459-e559-4b55-a0ef-02001190f5d8"),
            name: "Some project",
            type: ProjectType.Project)
        {
            Comment = "Some comment"
        };

        var mockLastProjectFunc = BuildMockLastProjectFunc(
            new LastProjectSetGetOut
            {
                Projects = [project]
            });

        var func = new AgentLastProjectSetGetFunc(mockLastProjectFunc.Object, SomeOption);
        var actual = await func.InvokeAsync(SomeContext, new(5), TestContext.Current.CancellationToken);

        var expected = new AgentLastProjectSetGetOut
        {
            Projects =
            [
                new(project.Id, project.Name, project.Type)
                {
                    Comment = project.Comment
                }
            ]
        };

        Assert.StrictEqual(expected, actual.SuccessOrThrow());
    }
}

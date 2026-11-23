using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTagSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ProjectIdIsEmpty_ExpectInvalidProjectIdFailure()
    {
        var mockTagFunc = BuildMockTagFunc(default(TagSetGetOut));
        var func = new AgentTagSetGetFunc(mockTagFunc.Object, new() { MaxTags = 2 });

        var actual = await func.InvokeAsync(SomeContext, new(Guid.Empty), TestContext.Current.CancellationToken);

        Assert.Equal(AgentTagSetGetFailureCode.InvalidProjectId, actual.FailureOrThrow().FailureCode);
        mockTagFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ValidInput_ExpectTrustedEntraObjectIdPassedToTagFunc()
    {
        var projectId = Guid.NewGuid();
        var mockTagFunc = BuildMockTagFunc(default(TagSetGetOut));
        var func = new AgentTagSetGetFunc(mockTagFunc.Object, new() { MaxTags = 2 });

        _ = await func.InvokeAsync(SomeContext, new(projectId), TestContext.Current.CancellationToken);

        var expected = new TagSetGetIn(SomeContext.EntraObjectId, projectId);
        mockTagFunc.Verify(f => f.InvokeAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_TagResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error");
        var mockTagFunc = BuildMockTagFunc(sourceException.ToFailure("Failed to get tags"));
        var func = new AgentTagSetGetFunc(mockTagFunc.Object, new() { MaxTags = 2 });

        var actual = await func.InvokeAsync(SomeContext, new(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var expected = Failure.Create(AgentTagSetGetFailureCode.Unknown, "Failed to get tags", sourceException);

        Assert.StrictEqual(expected, actual.FailureOrThrow());
    }

    [Fact]
    public static async Task InvokeAsync_TagResultIsSuccess_ExpectLimitedTags()
    {
        var mockTagFunc = BuildMockTagFunc(
            new TagSetGetOut
            {
                Tags = ["#first", "#second", "#third"]
            });

        var func = new AgentTagSetGetFunc(mockTagFunc.Object, new() { MaxTags = 2 });
        var actual = await func.InvokeAsync(SomeContext, new(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var expected = new AgentTagSetGetOut
        {
            Tags = ["#first", "#second"]
        };

        Assert.StrictEqual(expected, actual.SuccessOrThrow());
    }
}

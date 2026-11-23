using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentPeriodSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectPeriodFuncCalledOnce()
    {
        var mockPeriodFunc = BuildMockPeriodFunc(default(PeriodSetGetOut));
        var func = new AgentPeriodSetGetFunc(mockPeriodFunc.Object);

        _ = await func.InvokeAsync(SomeContext, TestContext.Current.CancellationToken);

        mockPeriodFunc.Verify(f => f.InvokeAsync(default, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_PeriodResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error");
        var mockPeriodFunc = BuildMockPeriodFunc(sourceException.ToFailure("Failed to get periods"));
        var func = new AgentPeriodSetGetFunc(mockPeriodFunc.Object);

        var actual = await func.InvokeAsync(SomeContext, TestContext.Current.CancellationToken);
        var expected = Failure.Create(
            AgentPeriodSetGetFailureCode.Unknown,
            "Failed to get periods",
            sourceException);

        Assert.StrictEqual(expected, actual.FailureOrThrow());
    }

    [Fact]
    public static async Task InvokeAsync_PeriodResultIsSuccess_ExpectMappedPeriods()
    {
        var period = new PeriodItem("Current month", new(2026, 9, 1), new(2026, 9, 28));
        var mockPeriodFunc = BuildMockPeriodFunc(
            new PeriodSetGetOut
            {
                Periods = [period]
            });

        var func = new AgentPeriodSetGetFunc(mockPeriodFunc.Object);
        var actual = await func.InvokeAsync(SomeContext, TestContext.Current.CancellationToken);
        var expected = new AgentPeriodSetGetOut
        {
            Periods = [new(period.Name, period.From, period.To)]
        };

        Assert.StrictEqual(expected, actual.SuccessOrThrow());
    }
}

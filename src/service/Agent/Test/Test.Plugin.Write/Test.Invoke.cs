using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentWritePluginTest
{
    [Fact]
    public static async Task PrepareCreateTimesheetAsync_InputIsValid_ExpectTrustedTypedInputAndCapture()
    {
        var prepareFunc = BuildPrepareFunc(SomeOutput);
        var capture = new AgentPreparedActionCapture();
        var plugin = CreatePlugin(prepareFunc, capture: capture);

        var actual = await plugin.PrepareCreateTimesheetAsync(
            "2026-09-29",
            SomeOutput.ProjectId,
            SomeOutput.Duration,
            SomeOutput.Description,
            TestContext.Current.CancellationToken);

        var result = Assert.IsType<AgentWriteToolResult<AgentTimesheetCreatePrepareOut>>(actual);
        Assert.True(result.IsSuccess);
        Assert.Equal(SomeOutput, result.Data);
        Assert.Equal(SomeOutput, capture.CreateAction);
        prepareFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                new AgentTimesheetCreatePrepareIn(
                    SomeOutput.Date,
                    SomeOutput.ProjectId,
                    SomeOutput.Duration,
                    SomeOutput.Description),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task PrepareDeleteTimesheetAsync_InputIsValid_ExpectTrustedTypedInputAndCapture()
    {
        var createPrepareFunc = new Mock<IAgentTimesheetCreatePrepareFunc>(MockBehavior.Strict);
        var deletePrepareFunc = BuildDeletePrepareFunc(SomeDeleteOutput);
        var capture = new AgentPreparedActionCapture();
        var plugin = CreatePlugin(createPrepareFunc, deletePrepareFunc, capture: capture);

        var actual = await plugin.PrepareDeleteTimesheetAsync(
            SomeDeleteOutput.TimesheetId,
            "2026-09-30",
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(SomeDeleteOutput, actual.Data);
        Assert.Equal(SomeDeleteOutput, capture.DeleteAction);
        deletePrepareFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                new AgentTimesheetDeletePrepareIn(SomeDeleteOutput.TimesheetId, SomeDeleteOutput.Date),
                It.IsAny<CancellationToken>()),
            Times.Once);
        createPrepareFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task PrepareUpdateTimesheetAsync_InputIsValid_ExpectTrustedTypedInputAndCapture()
    {
        var createPrepareFunc = new Mock<IAgentTimesheetCreatePrepareFunc>(MockBehavior.Strict);
        var updatePrepareFunc = BuildUpdatePrepareFunc(SomeUpdateOutput);
        var capture = new AgentPreparedActionCapture();
        var plugin = CreatePlugin(createPrepareFunc, updatePrepareFunc: updatePrepareFunc, capture: capture);

        var actual = await plugin.PrepareUpdateTimesheetAsync(
            SomeUpdateOutput.TimesheetId,
            "2026-09-30",
            "2026-09-28",
            SomeUpdateOutput.ProjectId,
            SomeUpdateOutput.Duration,
            SomeUpdateOutput.Description,
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(SomeUpdateOutput, actual.Data);
        Assert.Equal(SomeUpdateOutput, capture.UpdateAction);
        updatePrepareFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                new AgentTimesheetUpdatePrepareIn(
                    SomeUpdateOutput.TimesheetId,
                    new(2026, 09, 30),
                    SomeUpdateOutput.Date,
                    SomeUpdateOutput.ProjectId,
                    SomeUpdateOutput.Duration,
                    SomeUpdateOutput.Description),
                It.IsAny<CancellationToken>()),
            Times.Once);
        createPrepareFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("30.09.2026", null)]
    [InlineData("2026-09-30", "28.09.2026")]
    public static async Task PrepareUpdateTimesheetAsync_DateIsInvalid_ExpectSafeFailure(string sourceDate, string? date)
    {
        var createPrepareFunc = new Mock<IAgentTimesheetCreatePrepareFunc>(MockBehavior.Strict);
        var updatePrepareFunc = new Mock<IAgentTimesheetUpdatePrepareFunc>(MockBehavior.Strict);

        var actual = await CreatePlugin(createPrepareFunc, updatePrepareFunc: updatePrepareFunc).PrepareUpdateTimesheetAsync(
            SomeUpdateOutput.TimesheetId,
            sourceDate,
            date,
            null,
            2m,
            null,
            TestContext.Current.CancellationToken);

        Assert.False(actual.IsSuccess);
        Assert.Equal(nameof(AgentWriteToolFailureCode.InvalidDateFormat), actual.ErrorCode);
        updatePrepareFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("29.09.2026")]
    [InlineData("")]
    public static async Task PrepareCreateTimesheetAsync_DateIsInvalid_ExpectSafeFailure(string date)
    {
        var prepareFunc = new Mock<IAgentTimesheetCreatePrepareFunc>(MockBehavior.Strict);

        var actual = await CreatePlugin(prepareFunc).PrepareCreateTimesheetAsync(
            date,
            SomeOutput.ProjectId,
            SomeOutput.Duration,
            SomeOutput.Description,
            TestContext.Current.CancellationToken);

        Assert.False(actual.IsSuccess);
        Assert.Null(actual.Data);
        Assert.Equal(nameof(AgentWriteToolFailureCode.InvalidDateFormat), actual.ErrorCode);
        prepareFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task PrepareCreateTimesheetAsync_ActionIsAlreadyPrepared_ExpectNoSecondPreparation()
    {
        var prepareFunc = BuildPrepareFunc(SomeOutput);
        var plugin = CreatePlugin(prepareFunc);

        var first = await InvokeAsync();
        var second = await InvokeAsync();

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(nameof(AgentWriteToolFailureCode.ActionAlreadyPrepared), second.ErrorCode);
        prepareFunc.Verify(
            static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<AgentTimesheetCreatePrepareIn>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Task<AgentWriteToolResult<AgentTimesheetCreatePrepareOut>> InvokeAsync()
            =>
            plugin.PrepareCreateTimesheetAsync(
                "2026-09-29",
                SomeOutput.ProjectId,
                SomeOutput.Duration,
                SomeOutput.Description,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public static async Task KernelInvokeAsync_StringDateAndNumericValues_ExpectTypedPreparation()
    {
        var prepareFunc = BuildPrepareFunc(SomeOutput);
        var builder = Kernel.CreateBuilder();
        builder.Plugins.AddFromObject(CreatePlugin(prepareFunc), AgentWritePlugin.PluginName);
        var kernel = builder.Build();
        var arguments = new KernelArguments
        {
            ["date"] = "2026-09-29",
            ["projectId"] = SomeOutput.ProjectId,
            ["duration"] = SomeOutput.Duration,
            ["description"] = SomeOutput.Description
        };

        var actual = await kernel.InvokeAsync<AgentWriteToolResult<AgentTimesheetCreatePrepareOut>>(
            AgentWritePlugin.PluginName,
            "prepare_create_timesheet",
            arguments,
            TestContext.Current.CancellationToken);

        var result = Assert.IsType<AgentWriteToolResult<AgentTimesheetCreatePrepareOut>>(actual);
        Assert.True(result.IsSuccess);
        Assert.Equal(SomeOutput, result.Data);
        prepareFunc.VerifyAll();
    }
}

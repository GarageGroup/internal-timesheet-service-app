using System;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Action.Decide.Test;

public static partial class AgentActionDecideFuncTest
{
    private static readonly Guid SomeActionId = new("78302d93-e6dc-4fd6-be63-2480c8984382");

    private static readonly AgentActionDecideIn SomeInput = new(
        101,
        SomeActionId,
        303,
        202,
        202,
        AgentActionDecision.Confirm);

    private static readonly AgentUserContext SomeContext
        =
        new(
            101,
            202,
            202,
            new("80ae312e-305b-49dd-a905-d39e30d11385"),
            new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
            new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));

    private static AgentActionDecideFunc BuildFunc(
        in Result<AgentUserContext, Failure<AgentUserContextResolveFailureCode>> userResult,
        in Result<AgentTimesheetCreateConfirmOut, Failure<AgentTimesheetCreateConfirmFailureCode>> confirmResult,
        in Result<AgentTimesheetCreateCancelOut, Failure<AgentTimesheetCreateCancelFailureCode>> cancelResult,
        out Mock<IAgentUserContextResolver> resolver,
        out Mock<IAgentTimesheetCreateConfirmFunc> confirmFunc,
        out Mock<IAgentTimesheetCreateCancelFunc> cancelFunc,
        bool enabled = true)
    {
        resolver = new();
        confirmFunc = new();
        cancelFunc = new();
        var deleteConfirmFunc = new Mock<IAgentTimesheetDeleteConfirmFunc>();
        var deleteCancelFunc = new Mock<IAgentTimesheetDeleteCancelFunc>();
        var updateConfirmFunc = new Mock<IAgentTimesheetUpdateConfirmFunc>();
        var updateCancelFunc = new Mock<IAgentTimesheetUpdateCancelFunc>();

        _ = resolver
            .Setup(static r => r.ResolveAsync(
                It.IsAny<AgentUserIdentity>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userResult);

        _ = confirmFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmResult);

        _ = cancelFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelResult);

        _ = deleteConfirmFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentTimesheetDeleteConfirmFailureCode.NotFound, "Action not found"));

        _ = deleteCancelFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentTimesheetDeleteCancelFailureCode.NotFound, "Action not found"));

        _ = updateConfirmFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentTimesheetUpdateConfirmFailureCode.NotFound, "Action not found"));

        _ = updateCancelFunc
            .Setup(static f => f.InvokeAsync(
                It.IsAny<AgentUserContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failure.Create(AgentTimesheetUpdateCancelFailureCode.NotFound, "Action not found"));

        return new(
            resolver.Object,
            confirmFunc.Object,
            cancelFunc.Object,
            deleteConfirmFunc.Object,
            deleteCancelFunc.Object,
            updateConfirmFunc.Object,
            updateCancelFunc.Object,
            new(enabled));
    }
}

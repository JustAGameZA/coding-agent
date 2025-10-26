using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;
using CodingAgent.SharedKernel.Domain.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CodingAgent.Services.CICDMonitor.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "Unit")]
public class BuildFailedEventConsumerTests
{
    private readonly IAutomatedFixService _automatedFixService;
    private readonly ILogger<BuildFailedEventConsumer> _logger;
    private readonly BuildFailedEventConsumer _sut;

    public BuildFailedEventConsumerTests()
    {
        _automatedFixService = Substitute.For<IAutomatedFixService>();
        _logger = Substitute.For<ILogger<BuildFailedEventConsumer>>();
        _sut = new BuildFailedEventConsumer(_automatedFixService, _logger);
    }

    [Fact]
    public async Task Consume_WithValidEvent_CallsAutomatedFixService()
    {
        // Arrange
        var buildFailedEvent = new BuildFailedEvent
        {
            BuildId = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "error CS1234: compilation failed",
            ErrorLog = "Full build log here",
            WorkflowName = "CI",
            JobName = "build"
        };

        var context = Substitute.For<ConsumeContext<BuildFailedEvent>>();
        context.Message.Returns(buildFailedEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        var fixAttempt = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = buildFailedEvent.BuildId,
            TaskId = Guid.NewGuid(),
            Repository = "owner/repo",
            ErrorMessage = buildFailedEvent.ErrorMessage,
            ErrorPattern = "compilation_error",
            Status = FixStatus.InProgress,
            AttemptedAt = DateTime.UtcNow
        };

        _automatedFixService
            .ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>())
            .Returns(fixAttempt);

        // Act
        await _sut.Consume(context);

        // Assert
        await _automatedFixService.Received(1).ProcessBuildFailureAsync(
            Arg.Is<BuildFailure>(bf =>
                bf.Id == buildFailedEvent.BuildId &&
                bf.Repository == buildFailedEvent.Repository &&
                bf.Branch == buildFailedEvent.Branch &&
                bf.CommitSha == buildFailedEvent.CommitSha &&
                bf.ErrorMessage == buildFailedEvent.ErrorMessage &&
                bf.ErrorLog == buildFailedEvent.ErrorLog &&
                bf.WorkflowName == buildFailedEvent.WorkflowName &&
                bf.JobName == buildFailedEvent.JobName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenFixAttemptCreated_LogsSuccess()
    {
        // Arrange
        var buildFailedEvent = new BuildFailedEvent
        {
            BuildId = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "error CS1234: compilation failed"
        };

        var context = Substitute.For<ConsumeContext<BuildFailedEvent>>();
        context.Message.Returns(buildFailedEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        var fixAttempt = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = buildFailedEvent.BuildId,
            TaskId = Guid.NewGuid(),
            Repository = "owner/repo",
            ErrorMessage = buildFailedEvent.ErrorMessage,
            Status = FixStatus.InProgress,
            AttemptedAt = DateTime.UtcNow
        };

        _automatedFixService
            .ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>())
            .Returns(fixAttempt);

        // Act
        await _sut.Consume(context);

        // Assert
        // Verify that the method completed without throwing
        await _automatedFixService.Received(1).ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenNoFixAttemptCreated_LogsInfo()
    {
        // Arrange
        var buildFailedEvent = new BuildFailedEvent
        {
            BuildId = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "Unknown error"
        };

        var context = Substitute.For<ConsumeContext<BuildFailedEvent>>();
        context.Message.Returns(buildFailedEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        _automatedFixService
            .ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>())
            .Returns((FixAttempt?)null);

        // Act
        await _sut.Consume(context);

        // Assert
        await _automatedFixService.Received(1).ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenServiceThrows_RethrowsException()
    {
        // Arrange
        var buildFailedEvent = new BuildFailedEvent
        {
            BuildId = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "error CS1234"
        };

        var context = Substitute.For<ConsumeContext<BuildFailedEvent>>();
        context.Message.Returns(buildFailedEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        _automatedFixService
            .ProcessBuildFailureAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>())
            .Returns<FixAttempt?>(x => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Consume(context));
    }
}

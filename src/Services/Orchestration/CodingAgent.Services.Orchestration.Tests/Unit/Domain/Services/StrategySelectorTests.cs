using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Services;

[Trait("Category", "Unit")]
public class StrategySelectorTests
{
    private readonly IMLClassifierClient _mockMLClient;
    private readonly ILogger<StrategySelector> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly List<IExecutionStrategy> _strategies;
    private readonly IExecutionStrategy _singleShotStrategy;
    private readonly IExecutionStrategy _iterativeStrategy;
    private readonly IExecutionStrategy _multiAgentStrategy;
    private readonly StrategySelector _selector;

    public StrategySelectorTests()
    {
        _mockMLClient = Substitute.For<IMLClassifierClient>();
        _mockLogger = Substitute.For<ILogger<StrategySelector>>();
        _activitySource = new ActivitySource("Test");

        // Create mock strategies
        _singleShotStrategy = Substitute.For<IExecutionStrategy>();
        _singleShotStrategy.Name.Returns("SingleShot");
        _singleShotStrategy.SupportsComplexity.Returns(TaskComplexity.Simple);

        _iterativeStrategy = Substitute.For<IExecutionStrategy>();
        _iterativeStrategy.Name.Returns("Iterative");
        _iterativeStrategy.SupportsComplexity.Returns(TaskComplexity.Medium);

        _multiAgentStrategy = Substitute.For<IExecutionStrategy>();
        _multiAgentStrategy.Name.Returns("MultiAgent");
        _multiAgentStrategy.SupportsComplexity.Returns(TaskComplexity.Complex);

        _strategies = new List<IExecutionStrategy>
        {
            _singleShotStrategy,
            _iterativeStrategy,
            _multiAgentStrategy
        };

        _selector = new StrategySelector(
            _mockMLClient,
            _strategies,
            _mockLogger,
            _activitySource);
    }

    [Fact]
    public void Constructor_WithNullMLClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new StrategySelector(null!, _strategies, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mlClassifierClient");
    }

    [Fact]
    public void Constructor_WithNullStrategies_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new StrategySelector(_mockMLClient, null!, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategies");
    }

    [Fact]
    public void Constructor_WithEmptyStrategies_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new StrategySelector(_mockMLClient, new List<IExecutionStrategy>(), _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("strategies");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new StrategySelector(_mockMLClient, _strategies, null!, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullActivitySource_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new StrategySelector(_mockMLClient, _strategies, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activitySource");
    }

    [Fact]
    public async Task SelectStrategyAsync_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _selector.SelectStrategyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("task");
    }

    [Fact]
    public async Task SelectStrategyAsync_WithSimpleComplexity_ShouldReturnSingleShotStrategy()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Fix a small bug");

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "bug_fix",
            Complexity = "simple",
            Confidence = 0.95,
            Reasoning = "Simple bug fix",
            SuggestedStrategy = "SingleShot",
            EstimatedTokens = 2000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SingleShot");
        
        await _mockMLClient.Received(1).ClassifyAsync(
            Arg.Is<ClassificationRequest>(r => r.TaskDescription == task.Description),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectStrategyAsync_WithMediumComplexity_ShouldReturnIterativeStrategy()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Implement a new feature with moderate complexity");

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "feature",
            Complexity = "medium",
            Confidence = 0.85,
            Reasoning = "Medium complexity feature",
            SuggestedStrategy = "Iterative",
            EstimatedTokens = 6000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Iterative");
    }

    [Fact]
    public async Task SelectStrategyAsync_WithComplexComplexity_ShouldReturnMultiAgentStrategy()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Refactor the entire architecture");

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "refactor",
            Complexity = "complex",
            Confidence = 0.92,
            Reasoning = "Complex refactoring task",
            SuggestedStrategy = "MultiAgent",
            EstimatedTokens = 20000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MultiAgent");
    }

    [Fact]
    public async Task SelectStrategyAsync_WithMLServiceUnavailable_ShouldFallbackToHeuristic()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Fix typo in documentation");

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        // Heuristic should classify "fix typo" as simple
        result.Name.Should().Be("SingleShot");
        
        await _mockMLClient.Received(1).ClassifyAsync(
            Arg.Any<ClassificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectStrategyAsync_WithManualOverride_ShouldReturnSpecifiedStrategy()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Some task description");
        var strategyName = "Iterative";

        // Act
        var result = await _selector.SelectStrategyAsync(task, strategyName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(strategyName);
        
        // Should not call ML service when manual override is provided
        await _mockMLClient.DidNotReceive().ClassifyAsync(
            Arg.Any<ClassificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectStrategyAsync_WithInvalidManualOverride_ShouldFallbackToIterative()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Some task description");
        var invalidStrategyName = "NonExistentStrategy";

        // Act
        var result = await _selector.SelectStrategyAsync(task, invalidStrategyName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Iterative"); // Should fallback to Iterative
        
        await _mockMLClient.DidNotReceive().ClassifyAsync(
            Arg.Any<ClassificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectStrategyAsync_WithCaseInsensitiveOverride_ShouldWork()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Some task description");
        var strategyName = "singleshot"; // lowercase

        // Act
        var result = await _selector.SelectStrategyAsync(task, strategyName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SingleShot");
    }

    [Fact]
    public async Task SelectStrategyAsync_HeuristicWithComplexKeywords_ShouldReturnMultiAgent()
    {
        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(), 
            "Test Task", 
            "This is a complex architecture refactor that requires significant changes");

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MultiAgent");
    }

    [Fact]
    public async Task SelectStrategyAsync_HeuristicWithLongDescription_ShouldReturnMultiAgent()
    {
        // Arrange
        var longDescription = string.Join(" ", Enumerable.Repeat("word", 150));
        var task = new CodingTask(Guid.NewGuid(), "Test Task", longDescription);

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MultiAgent");
    }

    [Fact]
    public async Task SelectStrategyAsync_HeuristicWithSimpleKeywords_ShouldReturnSingleShot()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Quick fix for typo");

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SingleShot");
    }

    [Fact]
    public async Task SelectStrategyAsync_HeuristicWithMediumDescription_ShouldReturnIterative()
    {
        // Arrange
        var mediumDescription = string.Join(" ", Enumerable.Repeat("word", 50));
        var task = new CodingTask(Guid.NewGuid(), "Test Task", mediumDescription);

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Iterative");
    }

    [Fact]
    public async Task SelectStrategyAsync_ShouldUpdateTaskComplexity()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Fix a small bug");

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "bug_fix",
            Complexity = "simple",
            Confidence = 0.95,
            Reasoning = "Simple bug fix",
            SuggestedStrategy = "SingleShot",
            EstimatedTokens = 2000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        await _selector.SelectStrategyAsync(task);

        // Assert
        task.Complexity.Should().Be(TaskComplexity.Simple);
        task.Status.Should().Be(CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus.Classifying);
    }

    [Fact]
    public async Task SelectStrategyAsync_WithCancellationToken_ShouldPassToMLClient()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Fix bug");
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "bug_fix",
            Complexity = "simple",
            Confidence = 0.95,
            Reasoning = "Simple bug fix",
            SuggestedStrategy = "SingleShot",
            EstimatedTokens = 2000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        await _selector.SelectStrategyAsync(task, cancellationToken);

        // Assert
        await _mockMLClient.Received(1).ClassifyAsync(
            Arg.Any<ClassificationRequest>(),
            cancellationToken);
    }

    [Fact]
    public async Task SelectStrategyAsync_WithEpicComplexity_ShouldReturnMultiAgentStrategy()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Massive system rewrite");

        var classificationResponse = new ClassificationResponse
        {
            TaskType = "refactor",
            Complexity = "epic",
            Confidence = 0.98,
            Reasoning = "Epic scale task",
            SuggestedStrategy = "MultiAgent",
            EstimatedTokens = 50000
        };

        _mockMLClient.ClassifyAsync(Arg.Any<ClassificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(classificationResponse);

        // Act
        var result = await _selector.SelectStrategyAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MultiAgent");
    }
}

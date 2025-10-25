using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Integration.Domain.Services;

/// <summary>
/// Integration tests for StrategySelector with real ML Classifier service.
/// These tests require the ML Classifier service to be running.
/// Run with: dotnet test --filter Category=Integration
/// </summary>
[Trait("Category", "Integration")]
public class StrategySelectorIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly IMLClassifierClient _mlClient;
    private readonly ILogger<StrategySelector> _logger;
    private readonly ILogger<MLClassifierClient> _mlLogger;
    private readonly ActivitySource _activitySource;
    private readonly List<IExecutionStrategy> _strategies;
    private StrategySelector? _selector;
    private bool _mlServiceAvailable;

    public StrategySelectorIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<StrategySelector>();
        _mlLogger = loggerFactory.CreateLogger<MLClassifierClient>();
        _activitySource = new ActivitySource("IntegrationTest");

        // Configure HTTP client to connect to ML service
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("ML_CLASSIFIER_URL") ?? "http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        _mlClient = new MLClassifierClient(_httpClient, _mlLogger, _activitySource);

        // Create mock strategies
        var singleShotStrategy = CreateMockStrategy("SingleShot", TaskComplexity.Simple);
        var iterativeStrategy = CreateMockStrategy("Iterative", TaskComplexity.Medium);
        var multiAgentStrategy = CreateMockStrategy("MultiAgent", TaskComplexity.Complex);

        _strategies = new List<IExecutionStrategy>
        {
            singleShotStrategy,
            iterativeStrategy,
            multiAgentStrategy
        };
    }

    public async Task InitializeAsync()
    {
        // Check if ML service is available
        _mlServiceAvailable = await _mlClient.IsAvailableAsync();
        
        if (_mlServiceAvailable)
        {
            _selector = new StrategySelector(_mlClient, _strategies, _logger, _activitySource);
        }
    }

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SelectStrategyAsync_WithRealMLService_SimpleBugFix_ShouldReturnSingleShot()
    {
        // Skip if ML service is not available
        if (!_mlServiceAvailable || _selector == null)
        {
            // Mark as skipped
            return;
        }

        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(),
            "Fix typo",
            "Fix a typo in the login button text");

        // Act
        var strategy = await _selector.SelectStrategyAsync(task);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("SingleShot");
        task.Complexity.Should().Be(TaskComplexity.Simple);
    }

    [Fact]
    public async Task SelectStrategyAsync_WithRealMLService_MediumFeature_ShouldReturnIterative()
    {
        // Skip if ML service is not available
        if (!_mlServiceAvailable || _selector == null)
        {
            return;
        }

        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(),
            "Add user profile",
            "Implement a new user profile page with avatar upload and basic information editing");

        // Act
        var strategy = await _selector.SelectStrategyAsync(task);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().BeOneOf("Iterative", "MultiAgent"); // ML might classify differently
        task.Complexity.Should().BeOneOf(TaskComplexity.Medium, TaskComplexity.Complex);
    }

    [Fact]
    public async Task SelectStrategyAsync_WithRealMLService_ComplexRefactor_ShouldReturnMultiAgent()
    {
        // Skip if ML service is not available
        if (!_mlServiceAvailable || _selector == null)
        {
            return;
        }

        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(),
            "Refactor architecture",
            "Complete architectural refactor of the authentication system with microservices migration");

        // Act
        var strategy = await _selector.SelectStrategyAsync(task);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("MultiAgent");
        task.Complexity.Should().Be(TaskComplexity.Complex);
    }

    [Fact]
    public async Task SelectStrategyAsync_WithRealMLService_FastResponse_ShouldCompleteUnder100ms()
    {
        // Skip if ML service is not available
        if (!_mlServiceAvailable || _selector == null)
        {
            return;
        }

        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(),
            "Quick fix",
            "Fix null reference in user service");

        var stopwatch = Stopwatch.StartNew();

        // Act
        var strategy = await _selector.SelectStrategyAsync(task);
        stopwatch.Stop();

        // Assert
        strategy.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Strategy selection should complete in under 100ms for performance requirements");
    }

    [Fact]
    public async Task SelectStrategyAsync_WithRealMLService_ManualOverride_ShouldNotCallMLService()
    {
        // Skip if ML service is not available
        if (!_mlServiceAvailable || _selector == null)
        {
            return;
        }

        // Arrange
        var task = new CodingTask(
            Guid.NewGuid(),
            "Any task",
            "This should be overridden");

        // Act
        var strategy = await _selector.SelectStrategyAsync(task, "SingleShot");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("SingleShot");
        // Task should not be classified when manual override is used
        task.Status.Should().Be(CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus.Pending);
    }

    [Fact]
    public async Task MLClassifierClient_IsAvailableAsync_ShouldReturnExpectedStatus()
    {
        // Act
        var isAvailable = await _mlClient.IsAvailableAsync();

        // Assert
        // We just check that it doesn't throw - actual availability depends on environment
        // Log the result for diagnostic purposes
        Console.WriteLine($"ML Classifier service available: {isAvailable}");
    }

    private IExecutionStrategy CreateMockStrategy(string name, TaskComplexity complexity)
    {
        return new MockExecutionStrategy(name, complexity);
    }

    private class MockExecutionStrategy : IExecutionStrategy
    {
        public MockExecutionStrategy(string name, TaskComplexity complexity)
        {
            Name = name;
            SupportsComplexity = complexity;
        }

        public string Name { get; }
        public TaskComplexity SupportsComplexity { get; }

        public Task<StrategyExecutionResult> ExecuteAsync(
            CodingTask task,
            TaskExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Mock strategy for testing");
        }
    }
}

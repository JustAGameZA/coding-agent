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

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Strategies;

[Trait("Category", "Unit")]
public class IterativeStrategyTests
{
    private readonly ILlmClient _mockLlmClient;
    private readonly ICodeValidator _mockValidator;
    private readonly ILogger<IterativeStrategy> _mockLogger;
    private readonly IterativeStrategy _strategy;

    public IterativeStrategyTests()
    {
        _mockLlmClient = Substitute.For<ILlmClient>();
        _mockValidator = Substitute.For<ICodeValidator>();
        _mockLogger = Substitute.For<ILogger<IterativeStrategy>>();
        _strategy = new IterativeStrategy(_mockLlmClient, _mockValidator, _mockLogger);
    }

    [Fact]
    public void Constructor_WithNullLlmClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new IterativeStrategy(null!, _mockValidator, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("llmClient");
    }

    [Fact]
    public void Constructor_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new IterativeStrategy(_mockLlmClient, null!, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new IterativeStrategy(_mockLlmClient, _mockValidator, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Name_ShouldReturnIterative()
    {
        // Act & Assert
        _strategy.Name.Should().Be("Iterative");
    }

    [Fact]
    public void SupportsComplexity_ShouldReturnMedium()
    {
        // Act & Assert
        _strategy.SupportsComplexity.Should().Be(TaskComplexity.Medium);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = new TaskExecutionContext();

        // Act
        var act = async () => await _strategy.ExecuteAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("task");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var task = CreateTestTask();

        // Act
        var act = async () => await _strategy.ExecuteAsync(task, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task ExecuteAsync_SuccessOnFirstIteration_ShouldReturnSuccessResult()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/test.cs
```csharp
public class Test { }
```",
            TokensUsed = 150,
            Cost = 0.003m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Changes.Should().HaveCount(1);
        result.Changes[0].FilePath.Should().Be("src/test.cs");
        result.TotalTokensUsed.Should().Be(150);
        result.TotalCostUSD.Should().Be(0.003m);
        result.IterationsUsed.Should().Be(1);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Errors.Should().BeEmpty();

        await _mockLlmClient.Received(1).GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
        await _mockValidator.Received(1).ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FailsFirstThenSucceedsSecond_ShouldReturnSuccessWithTwoIterations()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var llmResponse1 = new LlmResponse
        {
            Content = @"FILE: src/test.cs
```csharp
public class Test { // Missing closing brace
```",
            TokensUsed = 100,
            Cost = 0.002m
        };

        var llmResponse2 = new LlmResponse
        {
            Content = @"FILE: src/test.cs
```csharp
public class Test { }
```",
            TokensUsed = 120,
            Cost = 0.0024m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse1, llmResponse2);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(
                ValidationResult.Failed("Syntax error: missing closing brace"),
                ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Changes.Should().HaveCount(1);
        result.TotalTokensUsed.Should().Be(220); // 100 + 120
        result.TotalCostUSD.Should().Be(0.0044m); // 0.002 + 0.0024
        result.IterationsUsed.Should().Be(2);

        await _mockLlmClient.Received(2).GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
        await _mockValidator.Received(2).ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FailsAllThreeIterations_ShouldReturnFailureResult()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/test.cs
```csharp
public class Test { // Always invalid
```",
            TokensUsed = 100,
            Cost = 0.002m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Failed("Syntax error: missing closing brace"));

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.TotalTokensUsed.Should().Be(300); // 100 * 3
        result.TotalCostUSD.Should().Be(0.006m); // 0.002 * 3
        result.IterationsUsed.Should().Be(3);
        result.Errors.Should().Contain(e => e.Contains("Max iterations"));
        result.Errors.Should().Contain("Syntax error: missing closing brace");

        await _mockLlmClient.Received(3).GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
        await _mockValidator.Received(3).ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NoCodeChangesInResponse_ShouldReturnFailureAfterMaxIterations()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = "Sorry, I cannot help with that.",
            TokensUsed = 50,
            Cost = 0.001m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Changes.Should().BeEmpty();
        result.IterationsUsed.Should().Be(3);
        result.Errors.Should().Contain(e => e.Contains("Failed to generate valid code changes"));

        await _mockLlmClient.Received(3).GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>());
        // Validator should never be called since no code changes were parsed
        await _mockValidator.DidNotReceive().ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldReturnCancelledResult()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();
        var cts = new CancellationTokenSource();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(callInfo =>
            {
                cts.Cancel();
                return new OperationCanceledException(cts.Token);
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cancelled"));
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldReturnTimeoutResult()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                // Simulate long-running operation
                var token = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(65), token);
                return new LlmResponse
                {
                    Content = "Too slow",
                    TokensUsed = 100,
                    Cost = 0.002m
                };
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("timed out"));
        result.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAsync_WithUnexpectedException_ShouldReturnFailureResult()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Unexpected error"));
    }

    [Fact]
    public async Task ExecuteAsync_WithRelevantFiles_ShouldIncludeInFirstPrompt()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext
        {
            RelevantFiles = new List<RelevantFile>
            {
                new RelevantFile { Path = "src/existing.cs", Content = "public class Existing { }" }
            }
        };

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/test.cs
```csharp
public class Test { }
```",
            TokensUsed = 150,
            Cost = 0.003m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        // Verify the LLM was called with a request containing file information
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(req =>
                req.Messages.Any(m => m.Role == "user" && m.Content.Contains("src/existing.cs"))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MultipleCodeChanges_ShouldParseAll()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/file1.cs
```csharp
public class File1 { }
```

FILE: src/file2.cs
```csharp
public class File2 { }
```",
            TokensUsed = 200,
            Cost = 0.004m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Changes.Should().HaveCount(2);
        result.Changes[0].FilePath.Should().Be("src/file1.cs");
        result.Changes[1].FilePath.Should().Be("src/file2.cs");
    }

    [Fact]
    public async Task ExecuteAsync_TracksCostAndTokensAcrossIterations()
    {
        // Arrange
        var task = CreateTestTask();
        var context = new TaskExecutionContext();

        var responses = new[]
        {
            new LlmResponse { Content = "FILE: test.cs\n```cs\nbad\n```", TokensUsed = 100, Cost = 0.002m },
            new LlmResponse { Content = "FILE: test.cs\n```cs\nalso bad\n```", TokensUsed = 120, Cost = 0.0024m },
            new LlmResponse { Content = "FILE: test.cs\n```cs\npublic class Good { }\n```", TokensUsed = 150, Cost = 0.003m }
        };

        var callCount = 0;
        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => responses[callCount++]);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(
                ValidationResult.Failed("Error 1"),
                ValidationResult.Failed("Error 2"),
                ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TotalTokensUsed.Should().Be(370); // 100 + 120 + 150
        result.TotalCostUSD.Should().Be(0.0074m); // 0.002 + 0.0024 + 0.003
        result.IterationsUsed.Should().Be(3);
    }

    private static CodingTask CreateTestTask()
    {
        var task = new CodingTask(
            userId: Guid.NewGuid(),
            title: "Test Task",
            description: "A test task for unit testing");
        
        task.Classify(TaskType.Feature, TaskComplexity.Medium);
        
        return task;
    }
}

using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Strategies;

[Trait("Category", "Unit")]
public class SingleShotStrategyTests
{
    private readonly ILlmClient _mockLlmClient;
    private readonly ICodeValidator _mockValidator;
    private readonly ILogger<SingleShotStrategy> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly SingleShotStrategy _strategy;

    public SingleShotStrategyTests()
    {
        _mockLlmClient = Substitute.For<ILlmClient>();
        _mockValidator = Substitute.For<ICodeValidator>();
        _mockLogger = Substitute.For<ILogger<SingleShotStrategy>>();
        _activitySource = new ActivitySource("Test");
        
        _strategy = new SingleShotStrategy(
            _mockLlmClient,
            _mockValidator,
            _mockLogger,
            _activitySource);
    }

    [Fact]
    public void Constructor_WithNullLlmClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new SingleShotStrategy(null!, _mockValidator, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("llmClient");
    }

    [Fact]
    public void Constructor_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new SingleShotStrategy(_mockLlmClient, null!, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new SingleShotStrategy(_mockLlmClient, _mockValidator, null!, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullActivitySource_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new SingleShotStrategy(_mockLlmClient, _mockValidator, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activitySource");
    }

    [Fact]
    public void Name_ShouldReturnSingleShot()
    {
        // Assert
        _strategy.Name.Should().Be("SingleShot");
    }

    [Fact]
    public void SupportsComplexity_ShouldReturnSimple()
    {
        // Assert
        _strategy.SupportsComplexity.Should().Be(TaskComplexity.Simple);
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
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix the login issue");

        // Act
        var act = async () => await _strategy.ExecuteAsync(task, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTask_ShouldCallLlmClient()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix login bug", "Users can't login");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();
        
        var llmResponse = new LlmResponse
        {
            Content = "FILE: test.cs\n```csharp\npublic class Test { }\n```",
            TokensUsed = 100,
            Cost = 0.02m,
            Model = "gpt-4o-mini"
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(r => r.Model == "gpt-4o-mini"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTask_ShouldReturnSuccessResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Add feature", "Add new validation");
        task.Classify(TaskType.Feature, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext
        {
            RelevantFiles = new List<RelevantFile>
            {
                new RelevantFile { Path = "src/Validator.cs", Content = "public class Validator { }", Language = "csharp" }
            }
        };

        var llmResponse = new LlmResponse
        {
            Content = "FILE: src/Validator.cs\n```csharp\npublic class Validator { public bool IsValid() => true; }\n```",
            TokensUsed = 150,
            Cost = 0.03m,
            Model = "gpt-4o-mini"
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
        result.TotalTokensUsed.Should().Be(150);
        result.TotalCostUSD.Should().Be(0.03m);
        result.Changes.Count.Should().Be(1);
        result.Changes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnFailedResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix validation");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = "FILE: test.cs\n```csharp\npublic class Test { }\n```",
            TokensUsed = 100,
            Cost = 0.02m,
            Model = "gpt-4o-mini"
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Failed("Syntax error", "Invalid code"));

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("validation", StringComparison.OrdinalIgnoreCase));
        result.TotalTokensUsed.Should().Be(100);
        result.TotalCostUSD.Should().Be(0.02m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCodeChangesParsed_ShouldReturnFailedResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix issue");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = "I cannot help with this task.",
            TokensUsed = 50,
            Cost = 0.01m,
            Model = "gpt-4o-mini"
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("No code changes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLlmClientThrows_ShouldReturnFailedResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix issue");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns<LlmResponse>(_ => throw new Exception("LLM service unavailable"));

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Execution failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldReturnCancelledResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix issue");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns<LlmResponse>(_ => throw new OperationCanceledException());

        // Act
        var result = await _strategy.ExecuteAsync(task, context, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeRelevantFilesInPrompt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Update feature", "Update validation logic");
        task.Classify(TaskType.Feature, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext
        {
            RelevantFiles = new List<RelevantFile>
            {
                new RelevantFile { Path = "src/Validator.cs", Content = "public class Validator { }", Language = "csharp" },
                new RelevantFile { Path = "src/Helper.cs", Content = "public class Helper { }", Language = "csharp" }
            }
        };

        var llmResponse = new LlmResponse
        {
            Content = "FILE: src/Validator.cs\n```csharp\npublic class Validator { public bool IsValid() => true; }\n```",
            TokensUsed = 200,
            Cost = 0.04m,
            Model = "gpt-4o-mini"
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(r => 
                r.Messages.Any(m => m.Content.Contains("src/Validator.cs")) &&
                r.Messages.Any(m => m.Content.Contains("src/Helper.cs"))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseCorrectTemperatureAndMaxTokens()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix bug", "Fix issue");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = "FILE: test.cs\n```csharp\npublic class Test { }\n```",
            TokensUsed = 100,
            Cost = 0.02m,
            Model = "gpt-4o-mini"
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        // Act
        await _strategy.ExecuteAsync(task, context);

        // Assert
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(r => 
                r.Temperature == 0.3 &&
                r.MaxTokens == 4000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldParseMultipleCodeChanges()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Update feature", "Update multiple files");
        task.Classify(TaskType.Feature, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/File1.cs
```csharp
public class File1 { }
```

FILE: src/File2.cs
```csharp
public class File2 { }
```",
            TokensUsed = 200,
            Cost = 0.04m,
            Model = "gpt-4o-mini"
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
        result.Changes.Count.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateLinesAddedCorrectly()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Add feature", "Add new class");
        task.Classify(TaskType.Feature, TaskComplexity.Simple);
        
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"FILE: src/NewClass.cs
```csharp
public class NewClass 
{ 
    public void Method1() { }
    public void Method2() { }
}
```",
            TokensUsed = 150,
            Cost = 0.03m,
            Model = "gpt-4o-mini"
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
        result.Changes.Any(c => !string.IsNullOrWhiteSpace(c.Content)).Should().BeTrue();
    }
}

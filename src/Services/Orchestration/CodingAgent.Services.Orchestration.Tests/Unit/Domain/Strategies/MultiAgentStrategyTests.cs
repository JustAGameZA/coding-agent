using System.Diagnostics;
using System.Text.Json;
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
public class MultiAgentStrategyTests
{
    private readonly IPlannerAgent _mockPlannerAgent;
    private readonly ICoderAgent _mockCoderAgent;
    private readonly IReviewerAgent _mockReviewerAgent;
    private readonly ITesterAgent _mockTesterAgent;
    private readonly ICodeValidator _mockValidator;
    private readonly ILogger<MultiAgentStrategy> _mockLogger;
    private readonly MultiAgentStrategy _strategy;

    public MultiAgentStrategyTests()
    {
        _mockPlannerAgent = Substitute.For<IPlannerAgent>();
        _mockCoderAgent = Substitute.For<ICoderAgent>();
        _mockReviewerAgent = Substitute.For<IReviewerAgent>();
        _mockTesterAgent = Substitute.For<ITesterAgent>();
        _mockValidator = Substitute.For<ICodeValidator>();
        _mockLogger = Substitute.For<ILogger<MultiAgentStrategy>>();

        _strategy = new MultiAgentStrategy(
            _mockPlannerAgent,
            _mockCoderAgent,
            _mockReviewerAgent,
            _mockTesterAgent,
            _mockValidator,
            _mockLogger);
    }

    [Fact]
    public void Constructor_WithNullPlannerAgent_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MultiAgentStrategy(
            null!,
            _mockCoderAgent,
            _mockReviewerAgent,
            _mockTesterAgent,
            _mockValidator,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("plannerAgent");
    }

    [Fact]
    public void Constructor_WithNullCoderAgent_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MultiAgentStrategy(
            _mockPlannerAgent,
            null!,
            _mockReviewerAgent,
            _mockTesterAgent,
            _mockValidator,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("coderAgent");
    }

    [Fact]
    public void Name_ShouldReturnMultiAgent()
    {
        // Assert
        _strategy.Name.Should().Be("MultiAgent");
    }

    [Fact]
    public void SupportsComplexity_ShouldReturnComplex()
    {
        // Assert
        _strategy.SupportsComplexity.Should().Be(TaskComplexity.Complex);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = new TaskExecutionContext();

        // Act
        Func<Task> act = async () => await _strategy.ExecuteAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("task");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");

        // Act
        Func<Task> act = async () => await _strategy.ExecuteAsync(task, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlannerFails_ShouldReturnFailure()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Planner",
                Success = false,
                Errors = new List<string> { "Planning failed" }
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Planning phase failed"));
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulPlan_ShouldExecuteCoderAgents()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();

        var plan = new TaskPlan
        {
            SubTasks = new List<SubTask>
            {
                new()
                {
                    Id = "subtask-1",
                    Title = "Subtask 1",
                    Description = "First subtask",
                    AffectedFiles = new List<string> { "file1.cs" },
                    EstimatedComplexity = 5
                }
            },
            Strategy = "Test strategy"
        };

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Planner",
                Success = true,
                Output = JsonSerializer.Serialize(plan),
                TokensUsed = 100,
                Cost = 0.01m
            });

        var codeChanges = new List<CodeChange>
        {
            new() { FilePath = "file1.cs", Content = "// code", Language = "csharp" }
        };

        _mockCoderAgent.ImplementSubTaskAsync(Arg.Any<SubTask>(), context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Coder-subtask-1",
                Success = true,
                Changes = codeChanges,
                TokensUsed = 200,
                Cost = 0.02m
            });

        var reviewResult = new ReviewResult
        {
            IsApproved = true,
            Severity = 1
        };

        _mockReviewerAgent.ReviewChangesAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Reviewer",
                Success = true,
                Output = JsonSerializer.Serialize(reviewResult),
                TokensUsed = 150,
                Cost = 0.015m
            });

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        _mockTesterAgent.GenerateTestsAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Tester",
                Success = true,
                Changes = new List<CodeChange>(),
                TokensUsed = 100,
                Cost = 0.01m
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Changes.Should().NotBeEmpty();
        result.TotalTokensUsed.Should().BeGreaterThan(0);
        result.TotalCostUSD.Should().BeGreaterThan(0);

        await _mockCoderAgent.Received(1).ImplementSubTaskAsync(
            Arg.Any<SubTask>(),
            context,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithReviewRejection_ShouldReturnFailure()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();

        var plan = new TaskPlan
        {
            SubTasks = new List<SubTask>
            {
                new()
                {
                    Id = "subtask-1",
                    Title = "Subtask 1",
                    Description = "First subtask",
                    AffectedFiles = new List<string> { "file1.cs" },
                    EstimatedComplexity = 5
                }
            },
            Strategy = "Test strategy"
        };

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Planner",
                Success = true,
                Output = JsonSerializer.Serialize(plan),
                TokensUsed = 100,
                Cost = 0.01m
            });

        _mockCoderAgent.ImplementSubTaskAsync(Arg.Any<SubTask>(), context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Coder-subtask-1",
                Success = true,
                Changes = new List<CodeChange>
                {
                    new() { FilePath = "file1.cs", Content = "// bad code", Language = "csharp" }
                },
                TokensUsed = 200,
                Cost = 0.02m
            });

        var reviewResult = new ReviewResult
        {
            IsApproved = false,
            Issues = new List<string> { "Security vulnerability found" },
            Severity = 5
        };

        _mockReviewerAgent.ReviewChangesAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Reviewer",
                Success = true,
                Output = JsonSerializer.Serialize(reviewResult),
                TokensUsed = 150,
                Cost = 0.015m
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Security vulnerability found"));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();

        var plan = new TaskPlan
        {
            SubTasks = new List<SubTask>
            {
                new()
                {
                    Id = "subtask-1",
                    Title = "Subtask 1",
                    Description = "First subtask",
                    AffectedFiles = new List<string> { "file1.cs" },
                    EstimatedComplexity = 5
                }
            },
            Strategy = "Test strategy"
        };

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Planner",
                Success = true,
                Output = JsonSerializer.Serialize(plan),
                TokensUsed = 100,
                Cost = 0.01m
            });

        _mockCoderAgent.ImplementSubTaskAsync(Arg.Any<SubTask>(), context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Coder-subtask-1",
                Success = true,
                Changes = new List<CodeChange>
                {
                    new() { FilePath = "file1.cs", Content = "// code", Language = "csharp" }
                },
                TokensUsed = 200,
                Cost = 0.02m
            });

        var reviewResult = new ReviewResult
        {
            IsApproved = true,
            Severity = 1
        };

        _mockReviewerAgent.ReviewChangesAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Reviewer",
                Success = true,
                Output = JsonSerializer.Serialize(reviewResult),
                TokensUsed = 150,
                Cost = 0.015m
            });

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Failed("Syntax error in file1.cs"));

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Syntax error"));
    }

    [Fact]
    public async Task ExecuteAsync_WithConflictingFiles_ShouldResolveConflicts()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();

        var plan = new TaskPlan
        {
            SubTasks = new List<SubTask>
            {
                new()
                {
                    Id = "subtask-1",
                    Title = "Subtask 1",
                    Description = "First subtask",
                    AffectedFiles = new List<string> { "file1.cs" },
                    EstimatedComplexity = 5
                },
                new()
                {
                    Id = "subtask-2",
                    Title = "Subtask 2",
                    Description = "Second subtask",
                    AffectedFiles = new List<string> { "file1.cs" }, // Same file
                    EstimatedComplexity = 5
                }
            },
            Strategy = "Test strategy"
        };

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Planner",
                Success = true,
                Output = JsonSerializer.Serialize(plan),
                TokensUsed = 100,
                Cost = 0.01m
            });

        var firstCall = true;
        _mockCoderAgent.ImplementSubTaskAsync(Arg.Any<SubTask>(), context, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var change = firstCall
                    ? new CodeChange { FilePath = "file1.cs", Content = "// first version", Language = "csharp" }
                    : new CodeChange { FilePath = "file1.cs", Content = "// second version", Language = "csharp" };
                firstCall = false;

                return new AgentResult
                {
                    AgentName = $"Coder-{callInfo.Arg<SubTask>().Id}",
                    Success = true,
                    Changes = new List<CodeChange> { change },
                    TokensUsed = 200,
                    Cost = 0.02m
                };
            });

        var reviewResult = new ReviewResult
        {
            IsApproved = true,
            Severity = 1
        };

        _mockReviewerAgent.ReviewChangesAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Reviewer",
                Success = true,
                Output = JsonSerializer.Serialize(reviewResult),
                TokensUsed = 150,
                Cost = 0.015m
            });

        _mockValidator.ValidateAsync(Arg.Any<List<CodeChange>>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        _mockTesterAgent.GenerateTestsAsync(Arg.Any<List<CodeChange>>(), task, Arg.Any<CancellationToken>())
            .Returns(new AgentResult
            {
                AgentName = "Tester",
                Success = true,
                Changes = new List<CodeChange>(),
                TokensUsed = 100,
                Cost = 0.01m
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context);

        // Assert
        result.Success.Should().BeTrue();
        // Should have only one change for file1.cs after conflict resolution
        result.Changes.Count(c => c.FilePath == "file1.cs").Should().Be(1);
        // Should use last-write-wins, so "second version"
        result.Changes.First(c => c.FilePath == "file1.cs").Content.Should().Contain("second version");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldReturnCancelledFailure()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test", "Test description");
        var context = new TaskExecutionContext();
        var cts = new CancellationTokenSource();

        _mockPlannerAgent.CreatePlanAsync(task, context, Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                cts.Cancel();
                await Task.Delay(10, callInfo.Arg<CancellationToken>());
                return new AgentResult
                {
                    AgentName = "Planner",
                    Success = true,
                    Output = "{\"subTasks\":[]}",
                    TokensUsed = 100,
                    Cost = 0.01m
                };
            });

        // Act
        var result = await _strategy.ExecuteAsync(task, context, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cancelled"));
    }
}

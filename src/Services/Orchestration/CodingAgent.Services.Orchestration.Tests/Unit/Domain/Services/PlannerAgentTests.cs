using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Services;

[Trait("Category", "Unit")]
public class PlannerAgentTests
{
    private readonly ILlmClient _mockLlmClient;
    private readonly ILogger<PlannerAgent> _mockLogger;
    private readonly PlannerAgent _agent;

    public PlannerAgentTests()
    {
        _mockLlmClient = Substitute.For<ILlmClient>();
        _mockLogger = Substitute.For<ILogger<PlannerAgent>>();
        _agent = new PlannerAgent(_mockLlmClient, _mockLogger);
    }

    [Fact]
    public void Constructor_WithNullLlmClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new PlannerAgent(null!, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("llmClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new PlannerAgent(_mockLlmClient, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task CreatePlanAsync_WithValidTask_ShouldReturnSuccessResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Test description");
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"```json
{
  ""subTasks"": [
    {
      ""id"": ""subtask-1"",
      ""title"": ""Implement feature"",
      ""description"": ""Add new functionality"",
      ""affectedFiles"": [""file1.cs""],
      ""estimatedComplexity"": 5,
      ""dependencies"": []
    }
  ],
  ""strategy"": ""Implement feature incrementally"",
  ""notes"": ""Start with tests""
}
```",
            TokensUsed = 200,
            Cost = 0.02m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _agent.CreatePlanAsync(task, context);

        // Assert
        result.Success.Should().BeTrue();
        result.AgentName.Should().Be("Planner");
        result.TokensUsed.Should().Be(200);
        result.Cost.Should().Be(0.02m);
        result.Output.Should().NotBeNullOrWhiteSpace();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlanAsync_WithInvalidJson_ShouldReturnDefaultPlan()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Test description");
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = "This is not valid JSON",
            TokensUsed = 100,
            Cost = 0.01m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _agent.CreatePlanAsync(task, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().NotBeNullOrWhiteSpace();
        result.Output.Should().Contain("default-1"); // Default subtask ID
    }

    [Fact]
    public async Task CreatePlanAsync_WhenLlmThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Test description");
        var context = new TaskExecutionContext();

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns<LlmResponse>(_ => throw new Exception("LLM API error"));

        // Act
        var result = await _agent.CreatePlanAsync(task, context);

        // Assert
        result.Success.Should().BeFalse();
        result.AgentName.Should().Be("Planner");
        result.Errors.Should().Contain("LLM API error");
    }

    [Fact]
    public async Task CreatePlanAsync_WithRelevantFiles_ShouldIncludeInPrompt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Test description");
        var context = new TaskExecutionContext
        {
            RelevantFiles = new List<RelevantFile>
            {
                new() { Path = "file1.cs", Content = "// existing code", Language = "csharp" }
            }
        };

        var llmResponse = new LlmResponse
        {
            Content = @"```json
{""subTasks"": [{""id"":""1"", ""title"":""Test"", ""description"":""Test"", ""affectedFiles"":[], ""estimatedComplexity"":5}], ""strategy"":""Test""}
```",
            TokensUsed = 200,
            Cost = 0.02m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _agent.CreatePlanAsync(task, context);

        // Assert
        result.Success.Should().BeTrue();

        // Verify LLM was called with file information
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(r => r.Messages.Any(m => m.Content.Contains("file1.cs"))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlanAsync_ShouldUseGpt4oModel()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Test Task", "Test description");
        var context = new TaskExecutionContext();

        var llmResponse = new LlmResponse
        {
            Content = @"```json
{""subTasks"": [{""id"":""1"", ""title"":""Test"", ""description"":""Test"", ""affectedFiles"":[], ""estimatedComplexity"":5}], ""strategy"":""Test""}
```",
            TokensUsed = 200,
            Cost = 0.02m
        };

        _mockLlmClient.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        await _agent.CreatePlanAsync(task, context);

        // Assert
        await _mockLlmClient.Received(1).GenerateAsync(
            Arg.Is<LlmRequest>(r => r.Model == "gpt-4o"),
            Arg.Any<CancellationToken>());
    }
}

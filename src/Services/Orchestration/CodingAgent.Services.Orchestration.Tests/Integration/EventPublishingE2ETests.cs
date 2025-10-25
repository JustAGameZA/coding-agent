using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Orchestration.Api.Endpoints;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

/// <summary>
/// Tests the complete execute flow with RabbitMQ messaging enabled.
/// Verifies task execution, persistence, and that the system runs correctly with messaging enabled.
/// Note: Actual message inspection requires RabbitMQ management API or consumer stubs,
/// which is beyond the scope of these tests. These tests verify the pipeline doesn't break.
/// </summary>
[Collection("OrchestrationWithRabbitMQCollection")]
[Trait("Category", "Integration")]
public class EventPublishingE2ETests
{
    private readonly OrchestrationWithRabbitMQFixture _fixture;
    private readonly ITestOutputHelper _output;

    public EventPublishingE2ETests(OrchestrationWithRabbitMQFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ExecuteTask_WithMessagingEnabled_ShouldCompleteSuccessfully()
    {
        // Skip if RabbitMQ is not available (Docker not running)
        if (!_fixture.IsRabbitMQAvailable)
        {
            _output.WriteLine("Skipping test: RabbitMQ container not available (Docker may not be running)");
            return;
        }

        _output.WriteLine("Test running with RabbitMQ messaging enabled");

        // Arrange: Create a task
        var createRequest = new
        {
            title = "RabbitMQ Messaging Test",
            description = "Verify execution works with messaging enabled"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = created!.Id;

        // Act: Execute the task
        var executeResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new
        {
            Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null
        });

        // Assert: Execution should succeed even with messaging enabled
        executeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteTaskResponse>();
        executeResult.Should().NotBeNull();
        executeResult!.TaskId.Should().Be(taskId);
        executeResult.ExecutionId.Should().NotBeEmpty();

        _output.WriteLine($"Task {taskId} execution queued successfully with messaging enabled");

        // Wait for execution to potentially complete
        await Task.Delay(1500);

        // Verify task state updated
        var getTaskResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}");
        var taskDetails = await getTaskResponse.Content.ReadFromJsonAsync<TaskDetailDto>();
        taskDetails.Should().NotBeNull();
        taskDetails!.Status.Should().NotBe(TaskStatus.Pending, 
            "task should have progressed beyond Pending state");

        _output.WriteLine($"Task status: {taskDetails.Status} (messaging integration verified)");
    }

    [Fact]
    public async Task ExecuteTask_WithMessaging_ShouldPersistExecutionRecords()
    {
        if (!_fixture.IsRabbitMQAvailable)
        {
            _output.WriteLine("Skipping test: RabbitMQ not available");
            return;
        }

        // Arrange: Create a task
        var createRequest = new
        {
            title = "Persistence with Messaging Test",
            description = "Verify execution records persist correctly with messaging"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = created!.Id;

        // Act: Execute
        var executeResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new
        {
            Strategy = CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy.SingleShot
        });
        executeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Wait for execution
        await Task.Delay(2000);

        // Assert: Execution record should be persisted
        var executionsResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}/executions");
        executionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var executions = await executionsResponse.Content.ReadFromJsonAsync<List<ExecutionDto>>();
        
        executions.Should().NotBeNull().And.NotBeEmpty();
        var execution = executions!.First();
        execution.Strategy.ToString().Should().Be("SingleShot");
        execution.StartedAt.Should().NotBe(default);

        _output.WriteLine($"Execution {execution.Id} persisted with strategy {execution.Strategy}, status {execution.Status}");
    }

    [Fact]
    public async Task ExecuteTask_MultipleWithMessaging_ShouldHandleConcurrency()
    {
        if (!_fixture.IsRabbitMQAvailable)
        {
            _output.WriteLine("Skipping test: RabbitMQ not available");
            return;
        }

        // Arrange: Create multiple tasks
        var tasks = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", new
            {
                title = $"Concurrent Test {i + 1}",
                description = $"Test concurrent execution with messaging - task {i + 1}"
            });
            var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
            tasks.Add(created!.Id);
        }

        // Act: Execute all tasks concurrently
        var executeTasks = tasks.Select(taskId =>
            _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new
            {
                Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null
            })).ToList();

        var results = await Task.WhenAll(executeTasks);

        // Assert: All should succeed
        results.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Accepted));

        _output.WriteLine($"Successfully queued {results.Length} concurrent executions with messaging enabled");

        // Verify all tasks progressed
        await Task.Delay(2000);

        foreach (var taskId in tasks)
        {
            var getResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}");
            var task = await getResponse.Content.ReadFromJsonAsync<TaskDetailDto>();
            task.Should().NotBeNull();
            task!.Status.Should().NotBe(TaskStatus.Pending);
            _output.WriteLine($"Task {taskId}: {task.Status}");
        }
    }

    [Fact]
    public async Task ExecuteTask_WithMessaging_ShouldStreamLogsViaSSE()
    {
        if (!_fixture.IsRabbitMQAvailable)
        {
            _output.WriteLine("Skipping test: RabbitMQ not available");
            return;
        }

        // Arrange: Create and execute a task
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", new
        {
            title = "SSE with Messaging Test",
            description = "Verify SSE works alongside messaging"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = created!.Id;

        await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new
        {
            Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null
        });

        // Act: Open SSE stream
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/tasks/{taskId}/logs");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var resp = await _fixture.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        // Assert: SSE should work with messaging enabled
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        _output.WriteLine("SSE streaming works correctly with RabbitMQ messaging enabled");
    }
}

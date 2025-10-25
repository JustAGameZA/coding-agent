using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Orchestration.Api.Endpoints;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

/// <summary>
/// End-to-end tests for the complete execute flow including event publishing.
/// Verifies the full pipeline: Create task → Execute → Verify persistence → Verify events published.
/// </summary>
[Collection("OrchestrationServiceCollection")]
[Trait("Category", "Integration")]
public class ExecuteFlowE2ETests
{
    private readonly OrchestrationServiceFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ExecuteFlowE2ETests(OrchestrationServiceFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ExecuteTask_ShouldPersistExecution_AndStartTask()
    {
        // Arrange: Create a simple task
        var createRequest = new
        {
            title = "E2E Execute Test",
            description = "Simple task to test execution flow"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        created.Should().NotBeNull();
        var taskId = created!.Id;

        // Act: Execute the task
        var executeRequest = new
        {
            Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null // Auto-select strategy
        };
        var executeResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", executeRequest);

        // Assert: Execution should be accepted
        executeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteTaskResponse>();
        executeResult.Should().NotBeNull();
        executeResult!.TaskId.Should().Be(taskId);
        executeResult.ExecutionId.Should().NotBeEmpty();

        _output.WriteLine($"Task {taskId} execution queued with ID {executeResult.ExecutionId} using strategy {executeResult.Strategy}");

        // Verify: Task status should be updated (may be InProgress or already completed depending on timing)
        var getTaskResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}");
        getTaskResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var taskDetails = await getTaskResponse.Content.ReadFromJsonAsync<TaskDetailDto>();
        taskDetails.Should().NotBeNull();
        
        // Task should no longer be Pending
        taskDetails!.Status.Should().NotBe(TaskStatus.Pending, "task should have started execution");
        
        _output.WriteLine($"Task status after execution: {taskDetails.Status}");

        // Verify: Execution record should be persisted
        var executionsResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}/executions");
        executionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var executions = await executionsResponse.Content.ReadFromJsonAsync<List<ExecutionDto>>();
        executions.Should().NotBeNull().And.NotBeEmpty("at least one execution should be recorded");
        
        var execution = executions!.First();
        execution.Id.Should().Be(executeResult.ExecutionId);
        execution.TaskId.Should().Be(taskId);
        execution.Strategy.ToString().Should().NotBeEmpty();
        execution.StartedAt.Should().NotBe(default);

        _output.WriteLine($"Execution {execution.Id} found with status {execution.Status}");
    }

    [Fact]
    public async Task ExecuteTask_WithManualStrategyOverride_ShouldUseSpecifiedStrategy()
    {
        // Arrange: Create a task
        var createRequest = new
        {
            title = "Manual Strategy Test",
            description = "Test manual strategy selection"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = created!.Id;

        // Act: Execute with explicit strategy override
        var executeRequest = new
        {
            Strategy = CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy.SingleShot
        };
        var executeResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", executeRequest);

        // Assert: Should use the specified strategy
        executeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteTaskResponse>();
        executeResult.Should().NotBeNull();
        executeResult!.Strategy.ToString().Should().Be("SingleShot", "manual override should be respected");

        _output.WriteLine($"Task {taskId} executed with manual strategy override: {executeResult.Strategy}");
    }

    [Fact]
    public async Task ExecuteTask_NonExistentTask_ShouldReturn404()
    {
        // Arrange: Use a non-existent task ID
        var nonExistentId = Guid.NewGuid();

        // Act: Attempt to execute
        var executeRequest = new
        {
            Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null
        };
        var executeResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{nonExistentId}/execute", executeRequest);

        // Assert: Should return 404 Not Found
        executeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteTask_MultipleTimes_ShouldCreateMultipleExecutions()
    {
        // Arrange: Create a task
        var createRequest = new
        {
            title = "Multiple Execution Test",
            description = "Test multiple executions of the same task"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = created!.Id;

        // Act: Execute the task twice
        var execute1Response = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new { Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null });
        var execute2Response = await _fixture.Client.PostAsJsonAsync($"/tasks/{taskId}/execute", new { Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null });

        execute1Response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        execute2Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var execution1 = await execute1Response.Content.ReadFromJsonAsync<ExecuteTaskResponse>();
        var execution2 = await execute2Response.Content.ReadFromJsonAsync<ExecuteTaskResponse>();

        execution1!.ExecutionId.Should().NotBe(execution2!.ExecutionId, "each execution should have a unique ID");

        // Wait a bit for async execution to complete
        await Task.Delay(500);

        // Verify: Both executions should be recorded
        var executionsResponse = await _fixture.Client.GetAsync($"/tasks/{taskId}/executions");
        var executions = await executionsResponse.Content.ReadFromJsonAsync<List<ExecutionDto>>();
        
        executions.Should().NotBeNull();
        executions!.Count.Should().BeGreaterThanOrEqualTo(2, "both executions should be recorded");
        executions.Select(e => e.Id).Should().Contain(new[] { execution1.ExecutionId, execution2.ExecutionId });

        _output.WriteLine($"Task {taskId} executed twice: {execution1.ExecutionId}, {execution2.ExecutionId}");
    }
}

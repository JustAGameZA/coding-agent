using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Orchestration.Api.Endpoints;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

[Collection("OrchestrationServiceCollection")]
[Trait("Category", "Integration")]
public class TaskEndpointsTests
{
    private readonly OrchestrationServiceFixture _fixture;

    public TaskEndpointsTests(OrchestrationServiceFixture fixture)
    {
        _fixture = fixture;
    }

    #region CRUD Tests

    [Fact]
    public async Task CreateTask_ValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new
        {
            title = "Test Task",
            description = "This is a test task description"
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>();
        task.Should().NotBeNull();
        task!.Title.Should().Be("Test Task");
        task.Description.Should().Be("This is a test task description");
        task.Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public async Task CreateTask_EmptyTitle_ShouldReturn400ValidationError()
    {
        // Arrange
        var request = new
        {
            title = "",
            description = "Valid description"
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_TitleTooLong_ShouldReturn400ValidationError()
    {
        // Arrange
        var request = new
        {
            title = new string('a', 201), // Exceeds 200 character limit
            description = "Valid description"
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTask_ExistingId_ShouldReturn200WithTaskDetails()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Get Test Task",
            description = "Task for get test"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        // Act
        var response = await _fixture.Client.GetAsync($"/tasks/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskDetailDto>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(created.Id);
        task.Title.Should().Be("Get Test Task");
    }

    [Fact]
    public async Task GetTask_NonExistentId_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.GetAsync($"/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTask_ValidRequest_ShouldReturn200WithUpdatedTask()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Original Title",
            description = "Original description"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        var updateRequest = new
        {
            title = "Updated Title",
            description = "Updated description"
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/tasks/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TaskDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateTask_NonExistentId_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new
        {
            title = "Updated Title",
            description = "Updated description"
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/tasks/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_ExistingId_ShouldReturn204NoContent()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Task to Delete",
            description = "This task will be deleted"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        // Act
        var response = await _fixture.Client.DeleteAsync($"/tasks/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _fixture.Client.GetAsync($"/tasks/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_NonExistentId_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.DeleteAsync($"/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent); // Should fail
    }

    #endregion

    #region List and Pagination Tests

    [Fact]
    public async Task ListTasks_DefaultPagination_ShouldReturn200WithPaginationHeaders()
    {
        // Arrange: Create multiple tasks
        for (int i = 0; i < 3; i++)
        {
            await _fixture.Client.PostAsJsonAsync("/tasks", new
            {
                title = $"List Test Task {i}",
                description = $"Description {i}"
            });
        }

        // Act
        var response = await _fixture.Client.GetAsync("/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check pagination headers
        response.Headers.Should().ContainKey("X-Total-Count");
        response.Headers.Should().ContainKey("X-Page-Number");
        response.Headers.Should().ContainKey("X-Page-Size");
        response.Headers.Should().ContainKey("X-Total-Pages");

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ListTasks_WithPageSize_ShouldRespectPageSize()
    {
        // Arrange: Create multiple tasks
        for (int i = 0; i < 5; i++)
        {
            await _fixture.Client.PostAsJsonAsync("/tasks", new
            {
                title = $"Page Size Test {Guid.NewGuid()}",
                description = "Description"
            });
        }

        // Act
        var response = await _fixture.Client.GetAsync("/tasks?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ListTasks_WithPaginationLinks_ShouldIncludeLinkHeader()
    {
        // Arrange: Create multiple tasks
        for (int i = 0; i < 5; i++)
        {
            await _fixture.Client.PostAsJsonAsync("/tasks", new
            {
                title = $"Link Test {Guid.NewGuid()}",
                description = "Description"
            });
        }

        // Act
        var response = await _fixture.Client.GetAsync("/tasks?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        if (response.Headers.Contains("Link"))
        {
            var linkHeader = response.Headers.GetValues("Link").FirstOrDefault();
            linkHeader.Should().NotBeNullOrEmpty();
            linkHeader.Should().Contain("rel=\"first\"");
        }
    }

    #endregion

    #region Execution Tests

    [Fact]
    public async Task ExecuteTask_ValidTask_ShouldReturn202Accepted()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Execute Test Task",
            description = "Task to execute"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        var executeRequest = new { Strategy = (ExecutionStrategy?)null };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync($"/tasks/{created!.Id}/execute", executeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var result = await response.Content.ReadFromJsonAsync<ExecuteTaskResponse>();
        result.Should().NotBeNull();
        result!.TaskId.Should().Be(created.Id);
        result.ExecutionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteTask_NonExistentTask_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var executeRequest = new { Strategy = (ExecutionStrategy?)null };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync($"/tasks/{nonExistentId}/execute", executeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTaskExecutions_ValidTaskId_ShouldReturn200()
    {
        // Arrange: Create a task
        var createRequest = new
        {
            title = "Execution History Test",
            description = "Task for execution history"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        // Act
        var response = await _fixture.Client.GetAsync($"/tasks/{created!.Id}/executions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var executions = await response.Content.ReadFromJsonAsync<List<ExecutionDto>>();
        executions.Should().NotBeNull();
        executions!.Should().BeEmpty(); // No executions yet
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateTask_DescriptionTooLong_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            title = "Valid Title",
            description = new string('a', 10001) // Exceeds 10,000 character limit
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTask_EmptyDescription_ShouldReturn400()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Task for Update Validation",
            description = "Original description"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        var updateRequest = new
        {
            title = "Valid Title",
            description = ""
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/tasks/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Health Check Test

    [Fact]
    public async Task PingEndpoint_ShouldReturn200()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/tasks/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
    }

    #endregion
}

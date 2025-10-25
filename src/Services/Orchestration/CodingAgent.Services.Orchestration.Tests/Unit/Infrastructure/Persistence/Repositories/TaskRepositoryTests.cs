using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Infrastructure.Persistence.Repositories;

[Trait("Category", "Unit")]
public class TaskRepositoryTests : IDisposable
{
    private readonly OrchestrationDbContext _context;
    private readonly TaskRepository _repository;

    public TaskRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrchestrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestrationDbContext(options);
        _repository = new TaskRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_WithValidTask_ShouldAddToDatabase()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix login bug", "Description");

        // Act
        var result = await _repository.AddAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(task.Id);
        _context.Tasks.Should().Contain(task);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ShouldReturnTask()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Fix login bug", "Description");
        await _repository.AddAsync(task);

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be(task.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenTasksExist_ShouldReturnAllTasks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task1 = new CodingTask(userId, "Task 1", "Description 1");
        var task2 = new CodingTask(userId, "Task 2", "Description 2");
        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == task1.Id);
        result.Should().Contain(t => t.Id == task2.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedTask_ShouldSaveChanges()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Original Title", "Description");
        await _repository.AddAsync(task);

        // Modify the task through domain methods
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);

        // Act
        await _repository.UpdateAsync(task);

        // Assert
        var updatedTask = await _repository.GetByIdAsync(task.Id);
        updatedTask.Should().NotBeNull();
        updatedTask!.Type.Should().Be(TaskType.BugFix);
        updatedTask.Complexity.Should().Be(TaskComplexity.Simple);
        updatedTask.Status.Should().Be(TaskStatus.Classifying);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_ShouldRemoveFromDatabase()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Task to delete", "Description");
        await _repository.AddAsync(task);

        // Act
        await _repository.DeleteAsync(task);

        // Assert
        var result = await _repository.GetByIdAsync(task.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenTaskExists_ShouldReturnTrue()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Existing Task", "Description");
        await _repository.AddAsync(task);

        // Act
        var exists = await _repository.ExistsAsync(task.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenTaskDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var task1 = new CodingTask(userId1, "User 1 Task 1", "Description");
        var task2 = new CodingTask(userId1, "User 1 Task 2", "Description");
        var task3 = new CodingTask(userId2, "User 2 Task", "Description");
        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);
        await _repository.AddAsync(task3);

        // Act
        var result = await _repository.GetByUserIdAsync(userId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == task1.Id);
        result.Should().Contain(t => t.Id == task2.Id);
        result.Should().NotContain(t => t.Id == task3.Id);
    }

    [Fact]
    public async Task GetByUserIdAndStatusAsync_ShouldReturnFilteredTasks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task1 = new CodingTask(userId, "Pending Task", "Description");
        var task2 = new CodingTask(userId, "Classifying Task", "Description");
        task2.Classify(TaskType.BugFix, TaskComplexity.Simple);
        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);

        // Act
        var pendingTasks = await _repository.GetByUserIdAndStatusAsync(userId, TaskStatus.Pending);
        var classifyingTasks = await _repository.GetByUserIdAndStatusAsync(userId, TaskStatus.Classifying);

        // Assert
        pendingTasks.Should().HaveCount(1);
        pendingTasks.First().Status.Should().Be(TaskStatus.Pending);
        classifyingTasks.Should().HaveCount(1);
        classifyingTasks.First().Status.Should().Be(TaskStatus.Classifying);
    }

    [Fact]
    public async Task GetWithExecutionsAsync_ShouldIncludeExecutions()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Task with executions", "Description");
        var execution = new TaskExecution(task.Id, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        task.AddExecution(execution);
        await _repository.AddAsync(task);

        // Act
        var result = await _repository.GetWithExecutionsAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Executions.Should().HaveCount(1);
        result.Executions.First().Strategy.Should().Be(ExecutionStrategy.SingleShot);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnTasksInRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task1 = new CodingTask(userId, "Old Task", "Description");
        var task2 = new CodingTask(userId, "New Task", "Description");
        await _repository.AddAsync(task1);
        await Task.Delay(100); // Ensure different timestamps
        await _repository.AddAsync(task2);

        var startDate = DateTime.UtcNow.AddMinutes(-1);
        var endDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CountByStatusAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task1 = new CodingTask(userId, "Pending Task 1", "Description");
        var task2 = new CodingTask(userId, "Pending Task 2", "Description");
        var task3 = new CodingTask(userId, "Classifying Task", "Description");
        task3.Classify(TaskType.BugFix, TaskComplexity.Simple);
        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);
        await _repository.AddAsync(task3);

        // Act
        var pendingCount = await _repository.CountByStatusAsync(userId, TaskStatus.Pending);
        var classifyingCount = await _repository.CountByStatusAsync(userId, TaskStatus.Classifying);

        // Assert
        pendingCount.Should().Be(2);
        classifyingCount.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _repository.DeleteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

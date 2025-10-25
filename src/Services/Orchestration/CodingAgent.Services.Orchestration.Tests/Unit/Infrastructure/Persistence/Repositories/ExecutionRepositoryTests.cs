using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Infrastructure.Persistence.Repositories;

[Trait("Category", "Unit")]
public class ExecutionRepositoryTests : IDisposable
{
    private readonly OrchestrationDbContext _context;
    private readonly ExecutionRepository _repository;
    private readonly Guid _taskId;

    public ExecutionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrchestrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestrationDbContext(options);
        _repository = new ExecutionRepository(_context);
        _taskId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_WithValidExecution_ShouldAddToDatabase()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var result = await _repository.AddAsync(execution);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(execution.Id);
        _context.Executions.Should().Contain(execution);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExecutionExists_ShouldReturnExecution()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution);

        // Act
        var result = await _repository.GetByIdAsync(execution.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(execution.Id);
        result.TaskId.Should().Be(_taskId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExecutionDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenExecutionsExist_ShouldReturnAllExecutions()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Id == execution1.Id);
        result.Should().Contain(e => e.Id == execution2.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedExecution_ShouldSaveChanges()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution);

        // Modify the execution
        execution.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));

        // Act
        await _repository.UpdateAsync(execution);

        // Assert
        var updatedExecution = await _repository.GetByIdAsync(execution.Id);
        updatedExecution.Should().NotBeNull();
        updatedExecution!.Status.Should().Be(ExecutionStatus.Success);
        updatedExecution.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenExecutionExists_ShouldRemoveFromDatabase()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution);

        // Act
        await _repository.DeleteAsync(execution);

        // Assert
        var result = await _repository.GetByIdAsync(execution.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenExecutionExists_ShouldReturnTrue()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution);

        // Act
        var exists = await _repository.ExistsAsync(execution.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenExecutionDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTaskIdAsync_ShouldReturnOnlyTaskExecutions()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var execution1 = new TaskExecution(task1Id, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var execution2 = new TaskExecution(task1Id, ExecutionStrategy.Iterative, "gpt-4o");
        var execution3 = new TaskExecution(task2Id, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);
        await _repository.AddAsync(execution3);

        // Act
        var result = await _repository.GetByTaskIdAsync(task1Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Id == execution1.Id);
        result.Should().Contain(e => e.Id == execution2.Id);
        result.Should().NotContain(e => e.Id == execution3.Id);
    }

    [Fact]
    public async Task GetWithResultAsync_ShouldIncludeResult()
    {
        // Arrange
        var execution = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        execution.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));
        await _repository.AddAsync(execution);

        // Act
        var result = await _repository.GetWithResultAsync(execution.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Result.Should().NotBeNull();
        result.Result!.Success.Should().BeTrue();
        result.Result.TokensUsed.Should().Be(1500);
    }

    [Fact]
    public async Task GetByStrategyAsync_ShouldReturnFilteredExecutions()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        var execution3 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);
        await _repository.AddAsync(execution3);

        // Act
        var singleShotExecutions = await _repository.GetByStrategyAsync(ExecutionStrategy.SingleShot);
        var iterativeExecutions = await _repository.GetByStrategyAsync(ExecutionStrategy.Iterative);

        // Assert
        singleShotExecutions.Should().HaveCount(2);
        iterativeExecutions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnFilteredExecutions()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        execution2.Complete(2000, 0.04m, TimeSpan.FromSeconds(30));
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);

        // Act
        var pendingExecutions = await _repository.GetByStatusAsync(ExecutionStatus.Pending);
        var successExecutions = await _repository.GetByStatusAsync(ExecutionStatus.Success);

        // Assert
        pendingExecutions.Should().HaveCount(1);
        pendingExecutions.First().Status.Should().Be(ExecutionStatus.Pending);
        successExecutions.Should().HaveCount(1);
        successExecutions.First().Status.Should().Be(ExecutionStatus.Success);
    }

    [Fact]
    public async Task GetLatestByTaskIdAsync_ShouldReturnMostRecentExecution()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        await _repository.AddAsync(execution1);
        
        await Task.Delay(100); // Ensure different timestamps
        
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        await _repository.AddAsync(execution2);

        // Act
        var result = await _repository.GetLatestByTaskIdAsync(_taskId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(execution2.Id);
    }

    [Fact]
    public async Task GetTotalCostByTaskIdAsync_ShouldCalculateSumCorrectly()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        execution1.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));
        
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        execution2.Complete(3000, 0.06m, TimeSpan.FromSeconds(45));
        
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);

        // Act
        var totalCost = await _repository.GetTotalCostByTaskIdAsync(_taskId);

        // Assert
        totalCost.Should().Be(0.09m);
    }

    [Fact]
    public async Task GetTotalTokensByTaskIdAsync_ShouldCalculateSumCorrectly()
    {
        // Arrange
        var execution1 = new TaskExecution(_taskId, ExecutionStrategy.SingleShot, "gpt-4o-mini");
        execution1.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));
        
        var execution2 = new TaskExecution(_taskId, ExecutionStrategy.Iterative, "gpt-4o");
        execution2.Complete(3000, 0.06m, TimeSpan.FromSeconds(45));
        
        await _repository.AddAsync(execution1);
        await _repository.AddAsync(execution2);

        // Act
        var totalTokens = await _repository.GetTotalTokensByTaskIdAsync(_taskId);

        // Assert
        totalTokens.Should().Be(4500);
    }

    [Fact]
    public async Task GetTotalCostByTaskIdAsync_WithNoResults_ShouldReturnZero()
    {
        // Arrange
        var nonExistentTaskId = Guid.NewGuid();

        // Act
        var totalCost = await _repository.GetTotalCostByTaskIdAsync(nonExistentTaskId);

        // Assert
        totalCost.Should().Be(0m);
    }

    [Fact]
    public async Task GetTotalTokensByTaskIdAsync_WithNoResults_ShouldReturnZero()
    {
        // Arrange
        var nonExistentTaskId = Guid.NewGuid();

        // Act
        var totalTokens = await _repository.GetTotalTokensByTaskIdAsync(nonExistentTaskId);

        // Assert
        totalTokens.Should().Be(0);
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

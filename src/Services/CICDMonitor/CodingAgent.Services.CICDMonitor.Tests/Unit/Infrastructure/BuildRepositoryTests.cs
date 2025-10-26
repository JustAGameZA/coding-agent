using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;
using CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodingAgent.Services.CICDMonitor.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class BuildRepositoryTests
{
    private readonly CICDMonitorDbContext _context;
    private readonly Mock<ILogger<BuildRepository>> _mockLogger;
    private readonly BuildRepository _sut;

    public BuildRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CICDMonitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CICDMonitorDbContext(options);
        _mockLogger = new Mock<ILogger<BuildRepository>>();
        _sut = new BuildRepository(_context, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new BuildRepository(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new BuildRepository(_context, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task AddAsync_ShouldAddBuildToDatabase()
    {
        // Arrange
        var build = CreateTestBuild();

        // Act
        var result = await _sut.AddAsync(build);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(build.Id);
        
        var savedBuild = await _context.Builds.FindAsync(build.Id);
        savedBuild.Should().NotBeNull();
        savedBuild!.WorkflowRunId.Should().Be(build.WorkflowRunId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBuildExists_ShouldReturnBuild()
    {
        // Arrange
        var build = CreateTestBuild();
        await _context.Builds.AddAsync(build);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(build.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(build.Id);
        result.WorkflowRunId.Should().Be(build.WorkflowRunId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBuildDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByWorkflowRunIdAsync_WhenBuildExists_ShouldReturnBuild()
    {
        // Arrange
        var build = CreateTestBuild();
        await _context.Builds.AddAsync(build);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByWorkflowRunIdAsync(build.WorkflowRunId);

        // Assert
        result.Should().NotBeNull();
        result!.WorkflowRunId.Should().Be(build.WorkflowRunId);
    }

    [Fact]
    public async Task GetRecentBuildsAsync_ShouldReturnBuildsForRepository()
    {
        // Arrange
        var build1 = CreateTestBuild(owner: "test-owner", repository: "test-repo");
        var build2 = CreateTestBuild(owner: "test-owner", repository: "test-repo");
        var build3 = CreateTestBuild(owner: "other-owner", repository: "other-repo");
        
        await _context.Builds.AddRangeAsync(build1, build2, build3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRecentBuildsAsync("test-owner", "test-repo");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b =>
        {
            b.Owner.Should().Be("test-owner");
            b.Repository.Should().Be("test-repo");
        });
    }

    [Fact]
    public async Task GetRecentBuildsAsync_ShouldLimitResults()
    {
        // Arrange
        for (int i = 0; i < 150; i++)
        {
            var build = CreateTestBuild(owner: "test-owner", repository: "test-repo");
            await _context.Builds.AddAsync(build);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRecentBuildsAsync("test-owner", "test-repo", limit: 50);

        // Assert
        result.Should().HaveCount(50);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBuild()
    {
        // Arrange
        var build = CreateTestBuild();
        await _context.Builds.AddAsync(build);
        await _context.SaveChangesAsync();

        build.Status = BuildStatus.Failure;
        build.Conclusion = "failure";
        build.ErrorMessages.Add("Test error");

        // Act
        await _sut.UpdateAsync(build);

        // Assert
        var updatedBuild = await _context.Builds.FindAsync(build.Id);
        updatedBuild.Should().NotBeNull();
        updatedBuild!.Status.Should().Be(BuildStatus.Failure);
        updatedBuild.Conclusion.Should().Be("failure");
        updatedBuild.ErrorMessages.Should().Contain("Test error");
    }

    [Fact]
    public async Task DeleteOldBuildsAsync_ShouldRemoveBuildsExceedingRetentionLimit()
    {
        // Arrange
        var owner = "test-owner";
        var repository = "test-repo";
        
        for (int i = 0; i < 120; i++)
        {
            var build = CreateTestBuild(owner: owner, repository: repository);
            build.CreatedAt = DateTime.UtcNow.AddDays(-i);
            await _context.Builds.AddAsync(build);
        }
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteOldBuildsAsync(owner, repository, retentionLimit: 100);

        // Assert
        var remainingBuilds = await _context.Builds
            .Where(b => b.Owner == owner && b.Repository == repository)
            .ToListAsync();
        
        remainingBuilds.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetAllRecentBuildsAsync_ShouldReturnBuildsAcrossAllRepositories()
    {
        // Arrange
        var build1 = CreateTestBuild(owner: "owner1", repository: "repo1");
        var build2 = CreateTestBuild(owner: "owner2", repository: "repo2");
        var build3 = CreateTestBuild(owner: "owner3", repository: "repo3");
        
        await _context.Builds.AddRangeAsync(build1, build2, build3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllRecentBuildsAsync(limit: 10);

        // Assert
        result.Should().HaveCount(3);
    }

    private Build CreateTestBuild(
        string owner = "test-owner",
        string repository = "test-repo",
        long workflowRunId = 12345)
    {
        return new Build
        {
            Id = Guid.NewGuid(),
            WorkflowRunId = workflowRunId,
            Owner = owner,
            Repository = repository,
            Branch = "main",
            CommitSha = "abc123",
            WorkflowName = "CI",
            Status = BuildStatus.Success,
            Conclusion = "success",
            WorkflowUrl = "https://github.com/test/repo/actions/runs/12345",
            ErrorMessages = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }
}

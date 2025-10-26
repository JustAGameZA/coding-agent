using CodingAgent.Services.GitHub.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;

namespace CodingAgent.Services.GitHub.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class GitHubServiceTests
{
    private readonly Mock<IGitHubClient> _mockClient;
    private readonly Mock<ILogger<GitHubService>> _mockLogger;

    public GitHubServiceTests()
    {
        _mockClient = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<GitHubService>>();
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new GitHubService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new GitHubService(_mockClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRepositoryAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var mockReposClient = new Mock<IRepositoriesClient>();
        mockReposClient
            .Setup(c => c.Create(It.IsAny<NewRepository>()))
            .ThrowsAsync(new ApiException("Test error", System.Net.HttpStatusCode.BadRequest));

        _mockClient.Setup(c => c.Repository).Returns(mockReposClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.CreateRepositoryAsync("test-repo");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create repository*");
    }

    [Fact]
    public async Task ListRepositoriesAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var mockReposClient = new Mock<IRepositoriesClient>();
        mockReposClient
            .Setup(c => c.GetAllForCurrent())
            .ThrowsAsync(new ApiException("Test error", System.Net.HttpStatusCode.Unauthorized));

        _mockClient.Setup(c => c.Repository).Returns(mockReposClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.ListRepositoriesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to list repositories*");
    }

    [Fact]
    public async Task GetRepositoryAsync_WhenNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockReposClient = new Mock<IRepositoriesClient>();
        mockReposClient
            .Setup(c => c.Get(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Not found", System.Net.HttpStatusCode.NotFound));

        _mockClient.Setup(c => c.Repository).Returns(mockReposClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.GetRepositoryAsync("owner", "repo");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Repository not found*");
    }

    [Fact]
    public async Task DeleteRepositoryAsync_WhenSuccessful_ShouldCallOctokit()
    {
        // Arrange
        var mockReposClient = new Mock<IRepositoriesClient>();
        mockReposClient
            .Setup(c => c.Delete(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockClient.Setup(c => c.Repository).Returns(mockReposClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await sut.DeleteRepositoryAsync("owner", "repo");

        // Assert
        mockReposClient.Verify(c => c.Delete("owner", "repo"), Times.Once);
    }

    [Fact]
    public async Task DeleteBranchAsync_WhenSuccessful_ShouldCallOctokit()
    {
        // Arrange
        var mockGitClient = new Mock<IGitDatabaseClient>();
        var mockReferencesClient = new Mock<IReferencesClient>();
        mockReferencesClient
            .Setup(c => c.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockClient.Setup(c => c.Git).Returns(mockGitClient.Object);
        mockGitClient.Setup(g => g.Reference).Returns(mockReferencesClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await sut.DeleteBranchAsync("owner", "repo", "feature-branch");

        // Assert
        mockReferencesClient.Verify(
            c => c.Delete("owner", "repo", "heads/feature-branch"),
            Times.Once);
    }

    [Fact]
    public async Task GetBranchAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var mockReposClient = new Mock<IRepositoriesClient>();
        var mockBranchesClient = new Mock<IRepositoryBranchesClient>();
        mockBranchesClient
            .Setup(c => c.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Not found", System.Net.HttpStatusCode.NotFound));

        _mockClient.Setup(c => c.Repository).Returns(mockReposClient.Object);
        mockReposClient.Setup(r => r.Branch).Returns(mockBranchesClient.Object);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        var result = await sut.GetBranchAsync("owner", "repo", "nonexistent");

        // Assert
        result.Should().BeNull();
    }
}

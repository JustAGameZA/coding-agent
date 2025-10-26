using CodingAgent.Services.GitHub.Domain.Entities;
using FluentAssertions;

namespace CodingAgent.Services.GitHub.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class RepositoryTests
{
    [Fact]
    public void Repository_ShouldInitializeWithDefaultValues()
    {
        // Act
        var repository = new Repository();

        // Assert
        repository.Id.Should().BeEmpty();
        repository.Owner.Should().BeEmpty();
        repository.Name.Should().BeEmpty();
        repository.FullName.Should().BeEmpty();
        repository.CloneUrl.Should().BeEmpty();
        repository.DefaultBranch.Should().Be("main");
        repository.IsPrivate.Should().BeFalse();
    }

    [Fact]
    public void Repository_ShouldAllowSettingProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var repository = new Repository
        {
            Id = id,
            GitHubId = 12345,
            Owner = "testuser",
            Name = "test-repo",
            FullName = "testuser/test-repo",
            Description = "Test description",
            CloneUrl = "https://github.com/testuser/test-repo.git",
            DefaultBranch = "develop",
            IsPrivate = true,
            CreatedAt = now,
            UpdatedAt = now,
            LastSyncedAt = now
        };

        // Assert
        repository.Id.Should().Be(id);
        repository.GitHubId.Should().Be(12345);
        repository.Owner.Should().Be("testuser");
        repository.Name.Should().Be("test-repo");
        repository.FullName.Should().Be("testuser/test-repo");
        repository.Description.Should().Be("Test description");
        repository.CloneUrl.Should().Be("https://github.com/testuser/test-repo.git");
        repository.DefaultBranch.Should().Be("develop");
        repository.IsPrivate.Should().BeTrue();
        repository.CreatedAt.Should().Be(now);
        repository.UpdatedAt.Should().Be(now);
        repository.LastSyncedAt.Should().Be(now);
    }
}

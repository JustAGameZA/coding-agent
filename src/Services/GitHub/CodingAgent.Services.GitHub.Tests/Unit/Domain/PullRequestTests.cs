using CodingAgent.Services.GitHub.Domain.Entities;
using FluentAssertions;

namespace CodingAgent.Services.GitHub.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class PullRequestTests
{
    [Fact]
    public void PullRequest_WhenCreated_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var pr = new PullRequest
        {
            Id = Guid.NewGuid(),
            GitHubId = 12345,
            Number = 42,
            Owner = "testowner",
            RepositoryName = "testrepo",
            Title = "Test PR",
            Body = "Test body",
            Head = "feature-branch",
            Base = "main",
            State = "open",
            IsMerged = false,
            IsDraft = false,
            Author = "testuser",
            Url = "https://api.github.com/pr/42",
            HtmlUrl = "https://github.com/testowner/testrepo/pull/42",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        pr.Should().NotBeNull();
        pr.Id.Should().NotBeEmpty();
        pr.GitHubId.Should().Be(12345);
        pr.Number.Should().Be(42);
        pr.Owner.Should().Be("testowner");
        pr.RepositoryName.Should().Be("testrepo");
        pr.Title.Should().Be("Test PR");
        pr.Body.Should().Be("Test body");
        pr.Head.Should().Be("feature-branch");
        pr.Base.Should().Be("main");
        pr.State.Should().Be("open");
        pr.IsMerged.Should().BeFalse();
        pr.IsDraft.Should().BeFalse();
        pr.Author.Should().Be("testuser");
        pr.Url.Should().Contain("api.github.com");
        pr.HtmlUrl.Should().Contain("github.com");
    }

    [Fact]
    public void PullRequest_WhenMerged_ShouldHaveMergedAtTimestamp()
    {
        // Arrange
        var mergedAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var pr = new PullRequest
        {
            Id = Guid.NewGuid(),
            GitHubId = 12345,
            Number = 42,
            Owner = "testowner",
            RepositoryName = "testrepo",
            Title = "Merged PR",
            Head = "feature",
            Base = "main",
            State = "closed",
            IsMerged = true,
            Author = "testuser",
            Url = "https://api.github.com/pr/42",
            HtmlUrl = "https://github.com/pr/42",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            MergedAt = mergedAt
        };

        // Assert
        pr.IsMerged.Should().BeTrue();
        pr.MergedAt.Should().NotBeNull();
        pr.MergedAt.Should().Be(mergedAt);
    }

    [Fact]
    public void PullRequest_WhenClosed_ShouldHaveClosedAtTimestamp()
    {
        // Arrange
        var closedAt = DateTime.UtcNow.AddHours(-2);

        // Act
        var pr = new PullRequest
        {
            Id = Guid.NewGuid(),
            GitHubId = 12345,
            Number = 42,
            Owner = "testowner",
            RepositoryName = "testrepo",
            Title = "Closed PR",
            Head = "feature",
            Base = "main",
            State = "closed",
            IsMerged = false,
            Author = "testuser",
            Url = "https://api.github.com/pr/42",
            HtmlUrl = "https://github.com/pr/42",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            ClosedAt = closedAt
        };

        // Assert
        pr.State.Should().Be("closed");
        pr.IsMerged.Should().BeFalse();
        pr.ClosedAt.Should().NotBeNull();
        pr.ClosedAt.Should().Be(closedAt);
    }

    [Fact]
    public void PullRequest_WhenDraft_ShouldHaveDraftFlag()
    {
        // Act
        var pr = new PullRequest
        {
            Id = Guid.NewGuid(),
            GitHubId = 12345,
            Number = 42,
            Owner = "testowner",
            RepositoryName = "testrepo",
            Title = "Draft PR",
            Head = "feature",
            Base = "main",
            State = "open",
            IsMerged = false,
            IsDraft = true,
            Author = "testuser",
            Url = "https://api.github.com/pr/42",
            HtmlUrl = "https://github.com/pr/42",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        pr.IsDraft.Should().BeTrue();
        pr.State.Should().Be("open");
    }

    [Fact]
    public void PullRequest_BodyCanBeNull()
    {
        // Act
        var pr = new PullRequest
        {
            Id = Guid.NewGuid(),
            GitHubId = 12345,
            Number = 42,
            Owner = "testowner",
            RepositoryName = "testrepo",
            Title = "PR without body",
            Body = null,
            Head = "feature",
            Base = "main",
            State = "open",
            IsMerged = false,
            Author = "testuser",
            Url = "https://api.github.com/pr/42",
            HtmlUrl = "https://github.com/pr/42",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        pr.Body.Should().BeNull();
        pr.Title.Should().NotBeNullOrEmpty();
    }
}

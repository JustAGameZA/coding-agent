using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;
using FluentAssertions;

namespace CodingAgent.Services.CICDMonitor.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class BuildTests
{
    [Fact]
    public void Build_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var build = new Build();

        // Assert
        build.ErrorMessages.Should().NotBeNull();
        build.ErrorMessages.Should().BeEmpty();
        build.Owner.Should().Be(string.Empty);
        build.Repository.Should().Be(string.Empty);
    }

    [Fact]
    public void Build_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workflowRunId = 12345L;
        var owner = "test-owner";
        var repository = "test-repo";
        var branch = "main";
        var commitSha = "abc123";
        var workflowName = "CI";
        var status = BuildStatus.Failure;
        var conclusion = "failure";
        var workflowUrl = "https://github.com/test/repo/actions/runs/12345";
        var errorMessages = new List<string> { "Error 1", "Error 2" };
        var now = DateTime.UtcNow;

        // Act
        var build = new Build
        {
            Id = id,
            WorkflowRunId = workflowRunId,
            Owner = owner,
            Repository = repository,
            Branch = branch,
            CommitSha = commitSha,
            WorkflowName = workflowName,
            Status = status,
            Conclusion = conclusion,
            WorkflowUrl = workflowUrl,
            ErrorMessages = errorMessages,
            CreatedAt = now,
            UpdatedAt = now,
            StartedAt = now,
            CompletedAt = now
        };

        // Assert
        build.Id.Should().Be(id);
        build.WorkflowRunId.Should().Be(workflowRunId);
        build.Owner.Should().Be(owner);
        build.Repository.Should().Be(repository);
        build.Branch.Should().Be(branch);
        build.CommitSha.Should().Be(commitSha);
        build.WorkflowName.Should().Be(workflowName);
        build.Status.Should().Be(status);
        build.Conclusion.Should().Be(conclusion);
        build.WorkflowUrl.Should().Be(workflowUrl);
        build.ErrorMessages.Should().BeEquivalentTo(errorMessages);
        build.CreatedAt.Should().Be(now);
        build.UpdatedAt.Should().Be(now);
        build.StartedAt.Should().Be(now);
        build.CompletedAt.Should().Be(now);
    }

    [Fact]
    public void Build_CompletedAt_CanBeNull()
    {
        // Arrange & Act
        var build = new Build
        {
            CompletedAt = null
        };

        // Assert
        build.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Build_StartedAt_CanBeNull()
    {
        // Arrange & Act
        var build = new Build
        {
            StartedAt = null
        };

        // Assert
        build.StartedAt.Should().BeNull();
    }

    [Fact]
    public void Build_ErrorMessages_CanBeModified()
    {
        // Arrange
        var build = new Build
        {
            ErrorMessages = new List<string> { "Error 1" }
        };

        // Act
        build.ErrorMessages.Add("Error 2");

        // Assert
        build.ErrorMessages.Should().HaveCount(2);
        build.ErrorMessages.Should().Contain("Error 1");
        build.ErrorMessages.Should().Contain("Error 2");
    }
}

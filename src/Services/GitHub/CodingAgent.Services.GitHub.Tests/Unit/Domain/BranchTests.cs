using CodingAgent.Services.GitHub.Domain.Entities;
using FluentAssertions;

namespace CodingAgent.Services.GitHub.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class BranchTests
{
    [Fact]
    public void Branch_ShouldInitializeWithDefaultValues()
    {
        // Act
        var branch = new Branch();

        // Assert
        branch.Name.Should().BeEmpty();
        branch.Sha.Should().BeEmpty();
        branch.Protected.Should().BeFalse();
    }

    [Fact]
    public void Branch_ShouldAllowSettingProperties()
    {
        // Act
        var branch = new Branch
        {
            Name = "main",
            Sha = "abc123def456",
            Protected = true
        };

        // Assert
        branch.Name.Should().Be("main");
        branch.Sha.Should().Be("abc123def456");
        branch.Protected.Should().BeTrue();
    }
}

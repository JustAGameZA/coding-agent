using CodingAgent.Services.Auth.Infrastructure.Security;
using FluentAssertions;

namespace CodingAgent.Services.Auth.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _passwordHasher;

    public BcryptPasswordHasherTests()
    {
        _passwordHasher = new BcryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldGenerateValidBcryptHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2a$"); // BCrypt prefix
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid_hash_format";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("VeryLongPasswordThatExceedsTypicalLengthLimitsButShouldStillWork123!@#$%^&*()")]
    public void HashPassword_WithVariousInputs_ShouldGenerateValidHashes(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        
        var verified = _passwordHasher.VerifyPassword(password, hash);
        verified.Should().BeTrue();
    }
}

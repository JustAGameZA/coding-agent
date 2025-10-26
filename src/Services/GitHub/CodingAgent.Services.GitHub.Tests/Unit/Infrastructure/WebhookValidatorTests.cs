using CodingAgent.Services.GitHub.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.GitHub.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class WebhookValidatorTests
{
    private const string TestSecret = "test-secret-key";
    private const string TestPayload = @"{""test"":""payload""}";

    [Fact]
    public void ValidateSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var validator = new WebhookValidator(TestSecret);
        
        // Calculate actual signature
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(TestSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(TestPayload));
        var actualSignature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

        // Act
        var result = validator.ValidateSignature(TestPayload, actualSignature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var validator = new WebhookValidator(TestSecret);
        var invalidSignature = "sha256=invalid";

        // Act
        var result = validator.ValidateSignature(TestPayload, invalidSignature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithEmptyPayload_ReturnsFalse()
    {
        // Arrange
        var validator = new WebhookValidator(TestSecret);
        var signature = "sha256=somesignature";

        // Act
        var result = validator.ValidateSignature("", signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithEmptySignature_ReturnsFalse()
    {
        // Arrange
        var validator = new WebhookValidator(TestSecret);

        // Act
        var result = validator.ValidateSignature(TestPayload, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithNullSignature_ReturnsFalse()
    {
        // Arrange
        var validator = new WebhookValidator(TestSecret);

        // Act
        var result = validator.ValidateSignature(TestPayload, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithDifferentSecret_ReturnsFalse()
    {
        // Arrange
        var validator1 = new WebhookValidator(TestSecret);
        var validator2 = new WebhookValidator("different-secret");
        
        // Generate signature with validator1
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(TestSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(TestPayload));
        var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

        // Act - validate with validator2 (different secret)
        var result = validator2.ValidateSignature(TestPayload, signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullSecret_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new WebhookValidator(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

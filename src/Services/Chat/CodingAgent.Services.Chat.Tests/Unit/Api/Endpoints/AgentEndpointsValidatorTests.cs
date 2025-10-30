using CodingAgent.Services.Chat.Api.Endpoints;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Api.Endpoints;

[Trait("Category", "Unit")]
public class AgentEndpointsValidatorTests
{
    [Fact]
    public void Validator_Should_Fail_When_Content_Is_Empty()
    {
        // Arrange
        var validator = new AgentResponseRequestValidator();
        var request = new AgentResponseRequest("");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validator_Should_Fail_When_Content_Is_Too_Long()
    {
        // Arrange
        var validator = new AgentResponseRequestValidator();
        var longContent = new string('x', 10_001);
        var request = new AgentResponseRequest(longContent);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validator_Should_Pass_When_Content_Is_Valid()
    {
        // Arrange
        var validator = new AgentResponseRequestValidator();
        var request = new AgentResponseRequest("Agent response text.");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}

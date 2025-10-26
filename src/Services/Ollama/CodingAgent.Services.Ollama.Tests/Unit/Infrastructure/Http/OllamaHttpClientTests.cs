using System.Net;
using CodingAgent.Services.Ollama.Domain.ValueObjects;
using CodingAgent.Services.Ollama.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CodingAgent.Services.Ollama.Tests.Unit.Infrastructure.Http;

[Trait("Category", "Unit")]
public class OllamaHttpClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<OllamaHttpClient>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly OllamaHttpClient _sut;

    public OllamaHttpClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        _loggerMock = new Mock<ILogger<OllamaHttpClient>>();
        _sut = new OllamaHttpClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task ListModelsAsync_WhenSuccessful_ShouldReturnModelList()
    {
        // Arrange
        var responseJson = """
        {
            "models": [
                {
                    "name": "codellama:13b",
                    "size": 13958643712,
                    "digest": "sha256:abc123",
                    "modified_at": "2025-10-26T10:00:00Z"
                },
                {
                    "name": "deepseek-coder:6.7b",
                    "size": 6771777536,
                    "digest": "sha256:def456",
                    "modified_at": "2025-10-26T11:00:00Z"
                }
            ]
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.ListModelsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("codellama:13b");
        result[0].Size.Should().Be(13958643712);
        result[1].Name.Should().Be("deepseek-coder:6.7b");
    }

    [Fact]
    public async Task ListModelsAsync_WhenNoModels_ShouldReturnEmptyList()
    {
        // Arrange
        var responseJson = """
        {
            "models": []
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.ListModelsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListModelsAsync_WhenOllamaUnavailable_ShouldThrowException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "Service Unavailable");

        // Act
        Func<Task> act = async () => await _sut.ListModelsAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Could not connect to Ollama backend");
    }

    [Fact]
    public async Task GenerateAsync_WhenSuccessful_ShouldReturnResponse()
    {
        // Arrange
        var request = new OllamaGenerateRequest
        {
            Model = "codellama:13b",
            Prompt = "Write a hello world function in Python",
            Temperature = 0.7f,
            MaxTokens = 2000
        };

        var responseJson = """
        {
            "model": "codellama:13b",
            "response": "def hello_world():\n    print('Hello, World!')",
            "done": true,
            "prompt_eval_count": 15,
            "eval_count": 25,
            "eval_duration": 2500000000,
            "total_duration": 3000000000
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().Be("codellama:13b");
        result.Response.Should().Contain("hello_world");
        result.PromptEvalCount.Should().Be(15);
        result.EvalCount.Should().Be(25);
        result.TotalTokens.Should().Be(40);
        result.EvalDurationMs.Should().BeApproximately(2500, 1);
        result.Done.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_WhenOllamaFails_ShouldThrowException()
    {
        // Arrange
        var request = new OllamaGenerateRequest
        {
            Model = "codellama:13b",
            Prompt = "Test prompt"
        };

        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        Func<Task> act = async () => await _sut.GenerateAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generation failed");
    }

    [Fact]
    public async Task IsHealthyAsync_WhenOllamaAvailable_ShouldReturnTrue()
    {
        // Arrange
        var responseJson = """
        {
            "models": []
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenOllamaUnavailable_ShouldReturnFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "Service Unavailable");

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(15, 25, 40)]
    [InlineData(100, 200, 300)]
    [InlineData(0, 50, 50)]
    public async Task GenerateAsync_TotalTokens_ShouldBeCalculatedCorrectly(
        int promptTokens, int responseTokens, int expectedTotal)
    {
        // Arrange
        var request = new OllamaGenerateRequest
        {
            Model = "test-model",
            Prompt = "Test"
        };

        var responseJson = $$"""
        {
            "model": "test-model",
            "response": "Test response",
            "done": true,
            "prompt_eval_count": {{promptTokens}},
            "eval_count": {{responseTokens}},
            "eval_duration": 1000000000,
            "total_duration": 2000000000
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.TotalTokens.Should().Be(expectedTotal);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}

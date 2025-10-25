using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Infrastructure.ExternalServices;

[Trait("Category", "Unit")]
public class MLClassifierClientTests
{
    private readonly ILogger<MLClassifierClient> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly HttpClient _httpClient;
    private readonly MLClassifierClient _client;

    public MLClassifierClientTests()
    {
        _mockLogger = Substitute.For<ILogger<MLClassifierClient>>();
        _activitySource = new ActivitySource("Test");
        _httpClient = new HttpClient(new MockHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:8000")
        };
        _client = new MLClassifierClient(_httpClient, _mockLogger, _activitySource);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MLClassifierClient(null!, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MLClassifierClient(_httpClient, null!, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullActivitySource_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MLClassifierClient(_httpClient, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activitySource");
    }

    [Fact]
    public async Task ClassifyAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _client.ClassifyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task ClassifyAsync_WithEmptyDescription_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new ClassificationRequest { TaskDescription = "" };

        // Act
        var act = async () => await _client.ClassifyAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task ClassifyAsync_WithValidRequest_ShouldReturnClassificationResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.ResponseContent = new ClassificationResponse
        {
            TaskType = "bug_fix",
            Complexity = "simple",
            Confidence = 0.95,
            Reasoning = "Test reasoning",
            SuggestedStrategy = "SingleShot",
            EstimatedTokens = 2000,
            ClassifierUsed = "heuristic"
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        var request = new ClassificationRequest
        {
            TaskDescription = "Fix the login bug"
        };

        // Act
        var result = await client.ClassifyAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TaskType.Should().Be("bug_fix");
        result.Complexity.Should().Be("simple");
        result.Confidence.Should().Be(0.95);
        result.SuggestedStrategy.Should().Be("SingleShot");
        result.GetComplexity().Should().Be(TaskComplexity.Simple);
    }

    [Fact]
    public async Task ClassifyAsync_WithHttpError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        var request = new ClassificationRequest
        {
            TaskDescription = "Fix the login bug"
        };

        // Act
        var act = async () => await client.ClassifyAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceHealthy_ShouldReturnTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.OK
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.ServiceUnavailable
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ThrowException = true
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            Delay = TimeSpan.FromSeconds(5)
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new MLClassifierClient(httpClient, _mockLogger, _activitySource);

        var request = new ClassificationRequest
        {
            TaskDescription = "Fix the login bug"
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var act = async () => await client.ClassifyAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

// Mock HTTP message handler for testing
public class MockHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public object? ResponseContent { get; set; }
    public bool ThrowException { get; set; }
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        if (ThrowException)
        {
            throw new HttpRequestException("Test exception");
        }

        var response = new HttpResponseMessage(StatusCode);

        if (ResponseContent != null && StatusCode == HttpStatusCode.OK)
        {
            response.Content = JsonContent.Create(ResponseContent, options: JsonOptions);
        }

        return response;
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Infrastructure.ExternalServices;

[Trait("Category", "Unit")]
public class GitHubClientTests
{
    private readonly ILogger<GitHubClient> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly HttpClient _httpClient;
    private readonly GitHubClient _client;

    public GitHubClientTests()
    {
        _mockLogger = Substitute.For<ILogger<GitHubClient>>();
        _activitySource = new ActivitySource("Test");
        _httpClient = new HttpClient(new MockGitHubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:5004")
        };
        _client = new GitHubClient(_httpClient, _mockLogger, _activitySource);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GitHubClient(null!, _mockLogger, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GitHubClient(_httpClient, null!, _activitySource);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullActivitySource_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GitHubClient(_httpClient, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activitySource");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithNullOwner_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _client.CreatePullRequestAsync(
            null!, "repo", "title", "body", "head", "base");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("owner");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithEmptyRepo_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _client.CreatePullRequestAsync(
            "owner", "", "title", "body", "head", "base");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("repo");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _client.CreatePullRequestAsync(
            "owner", "repo", "", "body", "head", "base");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithEmptyHead_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _client.CreatePullRequestAsync(
            "owner", "repo", "title", "body", "", "base");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("head");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithEmptyBase_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _client.CreatePullRequestAsync(
            "owner", "repo", "title", "body", "head", "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("base");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithValidRequest_ShouldReturnPullRequest()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler();
        handler.ResponseContent = new PullRequestResponse(
            GitHubId: 123456789,
            Number: 42,
            Owner: "owner",
            RepositoryName: "repo",
            Title: "Test PR",
            Body: "Test description",
            Head: "feature-branch",
            Base: "main",
            State: "open",
            IsMerged: false,
            IsDraft: false,
            Author: "test-user",
            Url: "https://api.github.com/repos/owner/repo/pulls/42",
            HtmlUrl: "https://github.com/owner/repo/pull/42",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            MergedAt: null,
            ClosedAt: null);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main");

        // Assert
        result.Should().NotBeNull();
        result.Number.Should().Be(42);
        result.Url.Should().Be("https://api.github.com/repos/owner/repo/pulls/42");
        result.HtmlUrl.Should().Be("https://github.com/owner/repo/pull/42");
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithDraftFlag_ShouldCreateDraftPR()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler();
        handler.ResponseContent = new PullRequestResponse(
            GitHubId: 123456789,
            Number: 42,
            Owner: "owner",
            RepositoryName: "repo",
            Title: "Test PR",
            Body: "Test description",
            Head: "feature-branch",
            Base: "main",
            State: "open",
            IsMerged: false,
            IsDraft: true,
            Author: "test-user",
            Url: "https://api.github.com/repos/owner/repo/pulls/42",
            HtmlUrl: "https://github.com/owner/repo/pull/42",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            MergedAt: null,
            ClosedAt: null);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main", isDraft: true);

        // Assert
        result.Should().NotBeNull();
        result.Number.Should().Be(42);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithHttpError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var act = async () => await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithNotFoundError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            StatusCode = HttpStatusCode.NotFound
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var act = async () => await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithTimeout_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            Delay = TimeSpan.FromSeconds(10)
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5004"),
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var act = async () => await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceHealthy_ShouldReturnTrue()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            StatusCode = HttpStatusCode.OK
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            StatusCode = HttpStatusCode.ServiceUnavailable
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            ThrowException = true
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new MockGitHubHttpMessageHandler
        {
            Delay = TimeSpan.FromSeconds(5)
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        var client = new GitHubClient(httpClient, _mockLogger, _activitySource);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var act = async () => await client.CreatePullRequestAsync(
            "owner", "repo", "Test PR", "Test description", "feature-branch", "main",
            cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

// Mock HTTP message handler for testing GitHub service
public class MockGitHubHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public object? ResponseContent { get; set; }
    public bool ThrowException { get; set; }
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
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

// Response model matching GitHub service API
public record PullRequestResponse(
    long GitHubId,
    int Number,
    string Owner,
    string RepositoryName,
    string Title,
    string? Body,
    string Head,
    string Base,
    string State,
    bool IsMerged,
    bool IsDraft,
    string Author,
    string Url,
    string HtmlUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? MergedAt,
    DateTime? ClosedAt);

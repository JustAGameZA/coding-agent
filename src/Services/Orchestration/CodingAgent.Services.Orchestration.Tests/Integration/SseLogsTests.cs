using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

[Collection("OrchestrationServiceCollection")]
[Trait("Category", "Integration")]
public class SseLogsTests
{
    private readonly OrchestrationServiceFixture _fixture;

    public SseLogsTests(OrchestrationServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LogsSse_ShouldReturnStream_WithInitialEvents()
    {
        // Arrange: Create and execute a task
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", new
        {
            title = "SSE Test",
            description = "Validate SSE stream"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Api.Endpoints.TaskDto>();
        created.Should().NotBeNull();

        var execResponse = await _fixture.Client.PostAsJsonAsync($"/tasks/{created!.Id}/execute", new { Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null });
        execResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Act: Open SSE stream
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/tasks/{created!.Id}/logs");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var resp = await _fixture.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        // Assert: status and content type
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        // Read a few lines from the stream to ensure events arrive
        await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lines = new List<string>();
        while (!cts.IsCancellationRequested && lines.Count < 10)
        {
            var line = await reader.ReadLineAsync(cts.Token) ?? string.Empty;
            if (!string.IsNullOrEmpty(line))
            {
                lines.Add(line);
                // Exit early if we see an SSE data line
                if (line.StartsWith("data:")) break;
            }
        }

        lines.Should().NotBeEmpty();
        lines.Any(l => l.StartsWith("data:")).Should().BeTrue("SSE stream should include at least one data line");
    }
}

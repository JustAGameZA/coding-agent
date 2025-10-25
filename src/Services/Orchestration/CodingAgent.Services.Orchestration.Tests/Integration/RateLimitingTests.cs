using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

[Collection("OrchestrationServiceCollection")]
[Trait("Category", "Integration")]
public class RateLimitingTests
{
    private readonly OrchestrationServiceFixture _fixture;

    public RateLimitingTests(OrchestrationServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteTask_ShouldReturn429_AfterTenRequestsPerUser()
    {
        // Arrange: Create a task first
        var createRequest = new
        {
            title = "Rate Limit Test",
            description = "Ensure 10 executions/hour per user"
        };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/tasks", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Api.Endpoints.TaskDto>();
        created.Should().NotBeNull();

        // Use a unique user id for this test so other tests don't interfere
        var userId = Guid.NewGuid().ToString();

        // Act: Execute the task 11 times as the same user
        HttpStatusCode lastStatus = HttpStatusCode.OK;
        for (int i = 1; i <= 11; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/tasks/{created!.Id}/execute")
            {
                Content = JsonContent.Create(new { Strategy = (CodingAgent.Services.Orchestration.Domain.ValueObjects.ExecutionStrategy?)null })
            };
            req.Headers.Add("X-User-Id", userId);
            var resp = await _fixture.Client.SendAsync(req);
            lastStatus = resp.StatusCode;

            if (i <= 10)
            {
                resp.StatusCode.Should().Be(HttpStatusCode.Accepted, $"call #{i} should be accepted");
            }
        }

        // Assert: The 11th request should be rate limited
        lastStatus.Should().Be(HttpStatusCode.TooManyRequests);
    }
}

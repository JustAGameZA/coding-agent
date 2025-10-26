using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CodingAgent.Services.GitHub.Domain.Services;
using CodingAgent.Services.GitHub.Domain.Webhooks;
using CodingAgent.Services.GitHub.Infrastructure;
using CodingAgent.SharedKernel.Abstractions;

namespace CodingAgent.Services.GitHub.Api.Endpoints;

/// <summary>
/// Webhook endpoints for handling GitHub webhook events.
/// </summary>
public static class WebhookEndpoints
{
    /// <summary>
    /// Maps webhook endpoints.
    /// </summary>
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks")
            .WithTags("Webhooks");

        group.MapPost("/github", HandleGitHubWebhook)
            .WithName("HandleGitHubWebhook")
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleGitHubWebhook(
        HttpContext context,
        IWebhookService webhookService,
        IEventPublisher eventPublisher,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("HandleGitHubWebhook");

        // Get webhook secret from configuration
        var webhookSecret = configuration["GitHub:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            logger.LogError("GitHub webhook secret is not configured");
            return Results.Problem("Webhook secret not configured", statusCode: StatusCodes.Status500InternalServerError);
        }

        // Read the request body
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        // Get signature from header
        var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Webhook request missing X-Hub-Signature-256 header");
            return Results.Unauthorized();
        }

        // Validate signature
        var validator = new WebhookValidator(webhookSecret);
        if (!validator.ValidateSignature(payload, signature))
        {
            logger.LogWarning("Invalid webhook signature");
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid signature");
            return Results.Unauthorized();
        }

        // Get webhook event type and delivery ID
        var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var deliveryId = context.Request.Headers["X-GitHub-Delivery"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        activity?.SetTag("webhook.event_type", eventType);
        activity?.SetTag("webhook.delivery_id", deliveryId);

        logger.LogInformation("Received GitHub webhook: {EventType}, Delivery ID: {DeliveryId}", eventType, deliveryId);

        try
        {
            // Process webhook based on event type
            switch (eventType?.ToLower())
            {
                case "push":
                    var pushPayload = JsonSerializer.Deserialize<PushWebhookPayload>(payload);
                    if (pushPayload == null)
                    {
                        logger.LogError("Failed to deserialize push webhook payload");
                        return Results.BadRequest("Invalid push payload");
                    }

                    var pushEvent = webhookService.ProcessPushWebhook(pushPayload, deliveryId);
                    await eventPublisher.PublishAsync(pushEvent, cancellationToken);
                    
                    logger.LogInformation("Published push event for {Owner}/{Repo}, branch {Branch}, commit {Sha}",
                        pushEvent.RepositoryOwner, pushEvent.RepositoryName, pushEvent.Branch, pushEvent.CommitSha);
                    break;

                case "pull_request":
                    var prPayload = JsonSerializer.Deserialize<PullRequestWebhookPayload>(payload);
                    if (prPayload == null)
                    {
                        logger.LogError("Failed to deserialize pull request webhook payload");
                        return Results.BadRequest("Invalid pull request payload");
                    }

                    var prEvent = webhookService.ProcessPullRequestWebhook(prPayload, deliveryId);
                    await eventPublisher.PublishAsync(prEvent, cancellationToken);
                    
                    logger.LogInformation("Published pull request event for {Owner}/{Repo}, PR #{Number}, action {Action}",
                        prEvent.RepositoryOwner, prEvent.RepositoryName, prEvent.PullRequestNumber, prEvent.Action);
                    break;

                case "issues":
                case "issue_comment":
                    var issuePayload = JsonSerializer.Deserialize<IssueWebhookPayload>(payload);
                    if (issuePayload == null)
                    {
                        logger.LogError("Failed to deserialize issue webhook payload");
                        return Results.BadRequest("Invalid issue payload");
                    }

                    var issueEvent = webhookService.ProcessIssueWebhook(issuePayload, deliveryId);
                    await eventPublisher.PublishAsync(issueEvent, cancellationToken);
                    
                    logger.LogInformation("Published issue event for {Owner}/{Repo}, issue #{Number}, action {Action}",
                        issueEvent.RepositoryOwner, issueEvent.RepositoryName, issueEvent.IssueNumber, issueEvent.Action);
                    break;

                default:
                    logger.LogInformation("Ignoring unsupported webhook event type: {EventType}", eventType);
                    break;
            }

            return Results.Accepted();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize webhook payload");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem("Error processing webhook", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

using CodingAgent.Services.GitHub.Domain.Services;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;

namespace CodingAgent.Services.GitHub.Api.Endpoints;

/// <summary>
/// Pull Request endpoints for GitHub operations
/// </summary>
public static class PullRequestEndpoints
{
    public static void MapPullRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pull-requests")
            .WithTags("Pull Requests");

        group.MapPost("", CreatePullRequest)
            .WithName("CreatePullRequest")
            .Produces<PullRequestResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("{owner}/{repo}/{number:int}", GetPullRequest)
            .WithName("GetPullRequest")
            .Produces<PullRequestResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{owner}/{repo}", ListPullRequests)
            .WithName("ListPullRequests")
            .Produces<IEnumerable<PullRequestResponse>>();

        group.MapPost("{owner}/{repo}/{number:int}/merge", MergePullRequest)
            .WithName("MergePullRequest")
            .Produces<PullRequestResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{owner}/{repo}/{number:int}/close", ClosePullRequest)
            .WithName("ClosePullRequest")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{owner}/{repo}/{number:int}/comments", AddComment)
            .WithName("AddComment")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{owner}/{repo}/{number:int}/request-review", RequestReview)
            .WithName("RequestReview")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{owner}/{repo}/{number:int}/approve", ApprovePullRequest)
            .WithName("ApprovePullRequest")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{owner}/{repo}/{number:int}/review", ReviewPullRequest)
            .WithName("ReviewPullRequest")
            .Produces<CodeReviewResultResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("template", GetPRTemplate)
            .WithName("GetPRTemplate")
            .Produces<PullRequestTemplateResponse>();
    }

    private static async Task<IResult> CreatePullRequest(
        CreatePullRequestRequest request,
        IGitHubService githubService,
        IEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        try
        {
            var pr = await githubService.CreatePullRequestAsync(
                request.Owner,
                request.Repo,
                request.Title,
                request.Body ?? string.Empty,
                request.Head,
                request.Base,
                request.IsDraft,
                cancellationToken);

            var response = MapToResponse(pr);

            // Publish event
            await eventPublisher.PublishAsync(new PullRequestCreatedEvent
            {
                PullRequestId = pr.Id,
                Number = pr.Number,
                RepositoryOwner = pr.Owner,
                RepositoryName = pr.RepositoryName,
                Title = pr.Title,
                Url = pr.HtmlUrl,
                Head = pr.Head,
                Base = pr.Base,
                Author = pr.Author
            }, cancellationToken);

            return Results.Created($"/pull-requests/{pr.Owner}/{pr.RepositoryName}/{pr.Number}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetPullRequest(
        string owner,
        string repo,
        int number,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var pr = await githubService.GetPullRequestAsync(owner, repo, number, cancellationToken);
            var response = MapToResponse(pr);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListPullRequests(
        string owner,
        string repo,
        string? state,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var prs = await githubService.ListPullRequestsAsync(owner, repo, state, cancellationToken);
            var response = prs.Select(MapToResponse);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> MergePullRequest(
        string owner,
        string repo,
        int number,
        MergePullRequestRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var pr = await githubService.MergePullRequestAsync(
                owner,
                repo,
                number,
                request.MergeMethod ?? "merge",
                request.CommitTitle,
                request.CommitMessage,
                cancellationToken);

            var response = MapToResponse(pr);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ClosePullRequest(
        string owner,
        string repo,
        int number,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.ClosePullRequestAsync(owner, repo, number, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AddComment(
        string owner,
        string repo,
        int number,
        AddCommentRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.AddCommentAsync(owner, repo, number, request.Body, cancellationToken);
            return Results.Created($"/pull-requests/{owner}/{repo}/{number}/comments", null);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RequestReview(
        string owner,
        string repo,
        int number,
        RequestReviewRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.RequestReviewAsync(owner, repo, number, request.Reviewers, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ApprovePullRequest(
        string owner,
        string repo,
        int number,
        ApprovePullRequestRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.ApprovePullRequestAsync(owner, repo, number, request.Body, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ReviewPullRequest(
        string owner,
        string repo,
        int number,
        ICodeReviewService codeReviewService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Analyze the PR
            var result = await codeReviewService.AnalyzePullRequestAsync(owner, repo, number, cancellationToken);

            // Post review comments
            await codeReviewService.PostReviewCommentsAsync(owner, repo, number, result, cancellationToken);

            // Return the review result
            var response = new CodeReviewResultResponse(
                result.RequestChanges,
                result.Summary,
                result.Issues.Select(i => new CodeReviewIssueResponse(
                    i.Severity,
                    i.IssueType,
                    i.FilePath,
                    i.LineNumber,
                    i.Description,
                    i.Suggestion
                )).ToList()
            );

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static Task<IResult> GetPRTemplate()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "PullRequestTemplate.md");
        
        string template = File.Exists(templatePath) 
            ? File.ReadAllText(templatePath)
            : GetDefaultTemplate();

        var response = new PullRequestTemplateResponse(template);
        return Task.FromResult(Results.Ok(response));
    }

    private static string GetDefaultTemplate()
    {
        return @"## Description
<!-- Describe your changes in detail -->

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Checklist
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Code follows project style guidelines
- [ ] Self-review completed

## Related Issues
<!-- Link to related issues using #issue_number -->";
    }

    private static PullRequestResponse MapToResponse(Domain.Entities.PullRequest pr)
    {
        return new PullRequestResponse(
            pr.GitHubId,
            pr.Number,
            pr.Owner,
            pr.RepositoryName,
            pr.Title,
            pr.Body,
            pr.Head,
            pr.Base,
            pr.State,
            pr.IsMerged,
            pr.IsDraft,
            pr.Author,
            pr.Url,
            pr.HtmlUrl,
            pr.CreatedAt,
            pr.UpdatedAt,
            pr.MergedAt,
            pr.ClosedAt);
    }
}

// Request/Response models
public record CreatePullRequestRequest(
    string Owner,
    string Repo,
    string Title,
    string? Body,
    string Head,
    string Base,
    bool IsDraft = false);

public record MergePullRequestRequest(
    string? MergeMethod = "merge",
    string? CommitTitle = null,
    string? CommitMessage = null);

public record AddCommentRequest(string Body);

public record RequestReviewRequest(IEnumerable<string> Reviewers);

public record ApprovePullRequestRequest(string? Body = null);

public record PullRequestTemplateResponse(string Template);

public record CodeReviewResultResponse(
    bool RequestChanges,
    string Summary,
    List<CodeReviewIssueResponse> Issues);

public record CodeReviewIssueResponse(
    string Severity,
    string IssueType,
    string FilePath,
    int? LineNumber,
    string Description,
    string? Suggestion);

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

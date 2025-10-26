using CodingAgent.Services.GitHub.Domain.Services;

namespace CodingAgent.Services.GitHub.Api.Endpoints;

/// <summary>
/// Repository endpoints for GitHub operations
/// </summary>
public static class RepositoryEndpoints
{
    public static void MapRepositoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/repositories")
            .WithTags("Repositories");

        group.MapPost("", CreateRepository)
            .WithName("CreateRepository")
            .Produces<RepositoryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("", ListRepositories)
            .WithName("ListRepositories")
            .Produces<IEnumerable<RepositoryResponse>>();

        group.MapGet("{owner}/{name}", GetRepository)
            .WithName("GetRepository")
            .Produces<RepositoryResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("{owner}/{name}", UpdateRepository)
            .WithName("UpdateRepository")
            .Produces<RepositoryResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{owner}/{name}", DeleteRepository)
            .WithName("DeleteRepository")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateRepository(
        CreateRepositoryRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = await githubService.CreateRepositoryAsync(
                request.Name,
                request.Description,
                request.IsPrivate,
                cancellationToken);

            var response = new RepositoryResponse(
                repository.GitHubId,
                repository.Owner,
                repository.Name,
                repository.FullName,
                repository.Description,
                repository.CloneUrl,
                repository.DefaultBranch,
                repository.IsPrivate,
                repository.CreatedAt,
                repository.UpdatedAt);

            return Results.Created($"/repositories/{repository.Owner}/{repository.Name}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListRepositories(
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var repositories = await githubService.ListRepositoriesAsync(cancellationToken);

            var response = repositories.Select(r => new RepositoryResponse(
                r.GitHubId,
                r.Owner,
                r.Name,
                r.FullName,
                r.Description,
                r.CloneUrl,
                r.DefaultBranch,
                r.IsPrivate,
                r.CreatedAt,
                r.UpdatedAt));

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRepository(
        string owner,
        string name,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = await githubService.GetRepositoryAsync(owner, name, cancellationToken);

            var response = new RepositoryResponse(
                repository.GitHubId,
                repository.Owner,
                repository.Name,
                repository.FullName,
                repository.Description,
                repository.CloneUrl,
                repository.DefaultBranch,
                repository.IsPrivate,
                repository.CreatedAt,
                repository.UpdatedAt);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRepository(
        string owner,
        string name,
        UpdateRepositoryRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = await githubService.UpdateRepositoryAsync(
                owner,
                name,
                request.Description,
                cancellationToken);

            var response = new RepositoryResponse(
                repository.GitHubId,
                repository.Owner,
                repository.Name,
                repository.FullName,
                repository.Description,
                repository.CloneUrl,
                repository.DefaultBranch,
                repository.IsPrivate,
                repository.CreatedAt,
                repository.UpdatedAt);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRepository(
        string owner,
        string name,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.DeleteRepositoryAsync(owner, name, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}

// Request/Response models
public record CreateRepositoryRequest(string Name, string? Description = null, bool IsPrivate = false);
public record UpdateRepositoryRequest(string? Description);
public record RepositoryResponse(
    long GitHubId,
    string Owner,
    string Name,
    string FullName,
    string? Description,
    string CloneUrl,
    string DefaultBranch,
    bool IsPrivate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

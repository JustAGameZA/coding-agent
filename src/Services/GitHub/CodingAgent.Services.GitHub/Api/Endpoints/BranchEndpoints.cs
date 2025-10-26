using CodingAgent.Services.GitHub.Domain.Services;

namespace CodingAgent.Services.GitHub.Api.Endpoints;

/// <summary>
/// Branch endpoints for GitHub operations
/// </summary>
public static class BranchEndpoints
{
    public static void MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/repositories/{owner}/{repo}/branches")
            .WithTags("Branches");

        group.MapPost("", CreateBranch)
            .WithName("CreateBranch")
            .Produces<BranchResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("", ListBranches)
            .WithName("ListBranches")
            .Produces<IEnumerable<BranchResponse>>();

        group.MapGet("{branchName}", GetBranch)
            .WithName("GetBranch")
            .Produces<BranchResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{branchName}", DeleteBranch)
            .WithName("DeleteBranch")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateBranch(
        string owner,
        string repo,
        CreateBranchRequest request,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var branch = await githubService.CreateBranchAsync(
                owner,
                repo,
                request.BranchName,
                request.SourceBranch,
                cancellationToken);

            var response = new BranchResponse(branch.Name, branch.Sha, branch.Protected);

            return Results.Created($"/repositories/{owner}/{repo}/branches/{branch.Name}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListBranches(
        string owner,
        string repo,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var branches = await githubService.ListBranchesAsync(owner, repo, cancellationToken);

            var response = branches.Select(b => new BranchResponse(b.Name, b.Sha, b.Protected));

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetBranch(
        string owner,
        string repo,
        string branchName,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            var branch = await githubService.GetBranchAsync(owner, repo, branchName, cancellationToken);

            if (branch == null)
            {
                return Results.NotFound(new { error = $"Branch '{branchName}' not found" });
            }

            var response = new BranchResponse(branch.Name, branch.Sha, branch.Protected);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteBranch(
        string owner,
        string repo,
        string branchName,
        IGitHubService githubService,
        CancellationToken cancellationToken)
    {
        try
        {
            await githubService.DeleteBranchAsync(owner, repo, branchName, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}

// Request/Response models
public record CreateBranchRequest(string BranchName, string SourceBranch);
public record BranchResponse(string Name, string Sha, bool Protected);

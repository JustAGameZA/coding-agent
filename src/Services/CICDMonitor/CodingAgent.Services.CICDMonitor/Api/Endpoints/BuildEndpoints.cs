using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.CICDMonitor.Api.Endpoints;

/// <summary>
/// API endpoints for build operations.
/// </summary>
public static class BuildEndpoints
{
    public static void MapBuildEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/builds")
            .WithTags("Builds");

        group.MapGet("", GetAllRecentBuilds)
            .WithName("GetAllRecentBuilds")
            .WithSummary("Gets all recent builds across all monitored repositories")
            .Produces<IEnumerable<BuildDto>>();

        group.MapGet("/{id:guid}", GetBuildById)
            .WithName("GetBuildById")
            .WithSummary("Gets a specific build by its ID")
            .Produces<BuildDto>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllRecentBuilds(
        [FromQuery] int limit = 100,
        [FromServices] IBuildRepository repository = null!,
        CancellationToken cancellationToken = default)
    {
        var builds = await repository.GetAllRecentBuildsAsync(limit, cancellationToken);
        var buildDtos = builds.Select(BuildDto.FromEntity);
        return Results.Ok(buildDtos);
    }

    private static async Task<IResult> GetBuildById(
        Guid id,
        [FromServices] IBuildRepository repository = null!,
        CancellationToken cancellationToken = default)
    {
        var build = await repository.GetByIdAsync(id, cancellationToken);
        
        if (build == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(BuildDto.FromEntity(build));
    }
}

/// <summary>
/// Data transfer object for Build entity.
/// </summary>
public record BuildDto
{
    public Guid Id { get; init; }
    public long WorkflowRunId { get; init; }
    public string Owner { get; init; } = string.Empty;
    public string Repository { get; init; } = string.Empty;
    public string Branch { get; init; } = string.Empty;
    public string CommitSha { get; init; } = string.Empty;
    public string WorkflowName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Conclusion { get; init; }
    public string WorkflowUrl { get; init; } = string.Empty;
    public List<string> ErrorMessages { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }

    public static BuildDto FromEntity(Domain.Entities.Build build)
    {
        return new BuildDto
        {
            Id = build.Id,
            WorkflowRunId = build.WorkflowRunId,
            Owner = build.Owner,
            Repository = build.Repository,
            Branch = build.Branch,
            CommitSha = build.CommitSha,
            WorkflowName = build.WorkflowName,
            Status = build.Status.ToString(),
            Conclusion = build.Conclusion,
            WorkflowUrl = build.WorkflowUrl,
            ErrorMessages = build.ErrorMessages,
            CreatedAt = build.CreatedAt,
            UpdatedAt = build.UpdatedAt,
            StartedAt = build.StartedAt,
            CompletedAt = build.CompletedAt
        };
    }
}

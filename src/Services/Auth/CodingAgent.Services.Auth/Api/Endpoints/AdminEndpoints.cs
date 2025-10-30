using System.Diagnostics;
using System.Security.Claims;
using CodingAgent.Services.Auth.Application.DTOs;
using CodingAgent.Services.Auth.Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Auth.Api.Endpoints;

/// <summary>
/// Admin endpoints for user management
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();

        group.MapGet("/users", GetUsers)
            .WithName("GetUsers")
            .Produces<UserListResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithSummary("Get paginated list of users")
            .WithDescription("Retrieve a paginated list of users with optional search and role filtering. Admin role required.");

        group.MapGet("/users/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .Produces<UserDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get user details by ID")
            .WithDescription("Retrieve detailed information about a specific user. Admin role required.");

        group.MapPut("/users/{id:guid}/roles", UpdateUserRoles)
            .WithName("UpdateUserRoles")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Update user roles")
            .WithDescription("Update the roles assigned to a user. Admin role required.");

        group.MapPut("/users/{id:guid}/activate", ActivateUser)
            .WithName("ActivateUser")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Activate user account")
            .WithDescription("Activate a deactivated user account. Admin role required.");

        group.MapPut("/users/{id:guid}/deactivate", DeactivateUser)
            .WithName("DeactivateUser")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Deactivate user account")
            .WithDescription("Deactivate a user account, preventing login. Admin role required.");
    }

    private static async Task<IResult> GetUsers(
        [AsParameters] UserListQuery query,
        IUserRepository repo,
        IValidator<UserListQuery> validator,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetUsers");
        activity?.SetTag("page", query.Page);
        activity?.SetTag("pageSize", query.PageSize);
        activity?.SetTag("hasSearch", !string.IsNullOrWhiteSpace(query.Search));
        activity?.SetTag("hasRoleFilter", !string.IsNullOrWhiteSpace(query.Role));

        // Validate query parameters
        var validationResult = await validator.ValidateAsync(query, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var (users, totalCount) = await repo.GetPagedAsync(
                query.Page,
                query.PageSize,
                query.Search,
                query.Role,
                ct);

            var userSummaries = users.Select(u => new UserSummaryDto(
                u.Id,
                u.Username,
                u.Email,
                u.GetRoles(),
                u.IsActive,
                u.CreatedAt
            )).ToList();

            var response = new UserListResponse(userSummaries, totalCount, query.Page, query.PageSize);

            logger.LogInformation(
                "Retrieved {Count} users (page {Page}/{TotalPages})",
                userSummaries.Count,
                query.Page,
                (int)Math.Ceiling((double)totalCount / query.PageSize));

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving users list");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        IUserRepository repo,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetUserById");
        activity?.SetTag("user.id", id);

        try
        {
            var user = await repo.GetByIdAsync(id, ct);

            if (user == null)
            {
                logger.LogWarning("User {UserId} not found", id);
                return Results.NotFound();
            }

            var userDetail = new UserDetailDto(
                user.Id,
                user.Username,
                user.Email,
                user.GetRoles(),
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                user.Sessions.Count
            );

            logger.LogInformation("Retrieved details for user {UserId} - {Username}", user.Id, user.Username);

            return Results.Ok(userDetail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> UpdateUserRoles(
        Guid id,
        UpdateUserRolesRequest request,
        IUserRepository repo,
        IValidator<UpdateUserRolesRequest> validator,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("UpdateUserRoles");
        activity?.SetTag("user.id", id);
        activity?.SetTag("roles", string.Join(",", request.Roles));

        // Validate request
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var targetUser = await repo.GetByIdAsync(id, ct);

            if (targetUser == null)
            {
                logger.LogWarning("User {UserId} not found for role update", id);
                return Results.NotFound();
            }

            // Get current admin's user ID
            var currentUserId = GetUserIdFromClaims(user);

            // Prevent admin from removing their own Admin role (last admin check)
            if (currentUserId == id && !request.Roles.Contains("Admin"))
            {
                logger.LogWarning("Admin {UserId} attempted to remove their own Admin role", id);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Operation",
                    Detail = "Cannot remove Admin role from your own account",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Check if this would remove the last admin (simple check - count admins)
            if (targetUser.GetRoles().Contains("Admin") && !request.Roles.Contains("Admin"))
            {
                var (allUsers, _) = await repo.GetPagedAsync(1, 1000, null, "Admin", ct);
                var adminCount = allUsers.Count(u => u.IsActive && u.GetRoles().Contains("Admin"));

                if (adminCount <= 1)
                {
                    logger.LogWarning("Attempted to remove the last active admin role from user {UserId}", id);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = "Cannot remove Admin role from the last active administrator",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            }

            // Update roles
            var rolesString = string.Join(",", request.Roles);
            targetUser.UpdateRoles(rolesString);
            await repo.UpdateAsync(targetUser, ct);

            logger.LogInformation(
                "Updated roles for user {UserId} - {Username} to {Roles}",
                targetUser.Id,
                targetUser.Username,
                rolesString);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating roles for user {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> ActivateUser(
        Guid id,
        IUserRepository repo,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("ActivateUser");
        activity?.SetTag("user.id", id);

        try
        {
            var user = await repo.GetByIdAsync(id, ct);

            if (user == null)
            {
                logger.LogWarning("User {UserId} not found for activation", id);
                return Results.NotFound();
            }

            if (user.IsActive)
            {
                logger.LogInformation("User {UserId} is already active", id);
                return Results.NoContent();
            }

            user.Activate();
            await repo.UpdateAsync(user, ct);

            logger.LogInformation("Activated user {UserId} - {Username}", user.Id, user.Username);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating user {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> DeactivateUser(
        Guid id,
        IUserRepository repo,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("DeactivateUser");
        activity?.SetTag("user.id", id);

        try
        {
            var targetUser = await repo.GetByIdAsync(id, ct);

            if (targetUser == null)
            {
                logger.LogWarning("User {UserId} not found for deactivation", id);
                return Results.NotFound();
            }

            // Get current admin's user ID
            var currentUserId = GetUserIdFromClaims(user);

            // Prevent admin from deactivating themselves
            if (currentUserId == id)
            {
                logger.LogWarning("Admin {UserId} attempted to deactivate their own account", id);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Operation",
                    Detail = "Cannot deactivate your own account",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Check if this would deactivate the last admin
            if (targetUser.GetRoles().Contains("Admin"))
            {
                var (allUsers, _) = await repo.GetPagedAsync(1, 1000, null, "Admin", ct);
                var activeAdminCount = allUsers.Count(u => u.IsActive && u.Id != id && u.GetRoles().Contains("Admin"));

                if (activeAdminCount == 0)
                {
                    logger.LogWarning("Attempted to deactivate the last active admin {UserId}", id);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = "Cannot deactivate the last active administrator",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            }

            if (!targetUser.IsActive)
            {
                logger.LogInformation("User {UserId} is already inactive", id);
                return Results.NoContent();
            }

            targetUser.Deactivate();
            await repo.UpdateAsync(targetUser, ct);

            logger.LogInformation("Deactivated user {UserId} - {Username}", targetUser.Id, targetUser.Username);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating user {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? user.FindFirst("uid")?.Value;

        return subClaim != null && Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}

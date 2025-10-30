using System.Diagnostics;
using System.Security.Claims;
using CodingAgent.Services.Auth.Application.DTOs;
using CodingAgent.Services.Auth.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Authenticate user and receive JWT tokens")
            .WithDescription("Login with username and password to receive access and refresh tokens.");

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("Register")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Register a new user account")
            .WithDescription("Create a new user account with username, email, and password.");

        group.MapPost("/refresh", RefreshToken)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Refresh access token using refresh token")
            .WithDescription("Exchange a valid refresh token for a new access token and refresh token.");

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get current authenticated user information")
            .WithDescription("Retrieve profile information for the currently authenticated user.");

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Revoke refresh token (logout)")
            .WithDescription("Invalidate the refresh token to log out the user.");

        group.MapPost("/change-password", ChangePassword)
            .RequireAuthorization()
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Change user password")
            .WithDescription("Change the password for the currently authenticated user.");
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService authService,
        IValidator<LoginRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("Login");
        activity?.SetTag("username", request.Username);

        // Validate request
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var response = await authService.LoginAsync(request, ipAddress, userAgent, ct);

            logger.LogInformation("User {Username} logged in successfully from {IpAddress}", 
                request.Username, ipAddress);

            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Login failed for user {Username}", request.Username);
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IAuthService authService,
        IValidator<RegisterRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("Register");
        activity?.SetTag("username", request.Username);
        activity?.SetTag("email", request.Email);

        // Validate request
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var response = await authService.RegisterAsync(request, ipAddress, userAgent, ct);

            logger.LogInformation("User {Username} registered successfully from {IpAddress}", 
                request.Username, ipAddress);

            return Results.Created($"/auth/me", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Registration failed for user {Username}", request.Username);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<IResult> RefreshToken(
        RefreshTokenRequest request,
        IAuthService authService,
        IValidator<RefreshTokenRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("RefreshToken");

        // Validate request
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var response = await authService.RefreshTokenAsync(request, ipAddress, userAgent, ct);

            logger.LogInformation("Token refreshed successfully from {IpAddress}", ipAddress);

            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Token refresh failed from {IpAddress}", 
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user,
        IAuthService authService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetCurrentUser");

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        activity?.SetTag("user.id", userId);

        var userDto = await authService.GetCurrentUserAsync(userId.Value, ct);

        if (userDto == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(userDto);
    }

    private static async Task<IResult> Logout(
        [FromHeader(Name = "X-Refresh-Token")] string? refreshToken,
        IAuthService authService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("Logout");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Missing Refresh Token",
                Detail = "X-Refresh-Token header is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await authService.RevokeTokenAsync(refreshToken, ct);

        logger.LogInformation("User logged out successfully");

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        ClaimsPrincipal user,
        IAuthService authService,
        IValidator<ChangePasswordRequest> validator,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("ChangePassword");

        var userId = GetUserIdFromClaims(user);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        activity?.SetTag("user.id", userId);

        // Validate request
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            await authService.ChangePasswordAsync(userId.Value, request, ct);

            logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Password change failed for user {UserId}", userId);
            return Results.Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Password change failed for user {UserId}", userId);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Password Change Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
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

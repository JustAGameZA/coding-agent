using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using CodingAgent.SharedKernel.Results;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CodingAgent.Services.Chat.Api.Endpoints;

/// <summary>
/// Endpoints for conversation management
/// </summary>
public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Conversations")
            .WithOpenApi()
            .RequireAuthorization(); // Require JWT authentication for all conversation endpoints

        group.MapGet("", GetConversations)
            .WithName("GetConversations")
            .WithDescription("Retrieve conversations with pagination and optional search. Use 'q' parameter for full-text search. Pagination: default page size 50, max 100.")
            .WithSummary("List or search conversations with pagination")
            .Produces<List<ConversationDto>>();

        group.MapGet("{id:guid}", GetConversation)
            .WithName("GetConversation")
            .WithDescription("Retrieve a specific conversation by its unique identifier")
            .WithSummary("Get conversation by ID")
            .Produces<ConversationDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateConversation)
            .WithName("CreateConversation")
            .WithDescription("Create a new conversation with the specified title. Title must be between 1 and 200 characters.")
            .WithSummary("Create a new conversation")
            .Produces<ConversationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("{id}", UpdateConversation)
            .WithName("UpdateConversation")
            .WithDescription("Update the title of an existing conversation. Title must be between 1 and 200 characters.")
            .WithSummary("Update conversation title")
            .Produces<ConversationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("{id:guid}", DeleteConversation)
            .WithName("DeleteConversation")
            .WithDescription("Delete a conversation by its unique identifier")
            .WithSummary("Delete conversation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{id:guid}/messages", GetMessages)
            .WithName("GetMessages")
            .WithDescription("Retrieve messages for a conversation with cursor-based pagination")
            .WithSummary("Get messages by conversation ID")
            .Produces<PagedMessagesResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetConversations(
        string? q,
        IConversationRepository repository, 
        ILogger<Program> logger, 
        HttpContext httpContext,
        int page = 1, 
        int pageSize = 50,
        CancellationToken ct = default)
    {
        // Extract userId from JWT claims - try multiple claim names
        var userIdClaim = httpContext.User.FindFirst("sub") 
                         ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            logger.LogWarning("GetConversations: missing user id claim");
            return Results.Forbid();
        }

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("GetConversations: invalid user id claim value {UserId}", userIdClaim.Value);
            return Results.Forbid();
        }

        logger.LogInformation("Getting conversations for user {UserId} (page: {Page}, pageSize: {PageSize}, query: {Query})", userId, page, pageSize, q);
        
        var pagination = new PaginationParameters(page, pageSize);
        PagedResult<Conversation> pagedResult;
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Search with pagination - filtered by userId
            pagedResult = await repository.SearchPagedAsync(userId, q, pagination, ct);
        }
        else
        {
            // Get all conversations for this user with pagination
            pagedResult = await repository.GetPagedAsync(userId, pagination, ct);
        }
        
        var items = pagedResult.Items.Select(c => new ConversationDto
        {
            Id = c.Id,
            UserId = c.UserId,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Messages = new List<MessageDto>()
        }).ToList();

        // Add pagination metadata to response headers
        httpContext.Response.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
        httpContext.Response.Headers["X-Page-Number"] = pagedResult.PageNumber.ToString();
        httpContext.Response.Headers["X-Page-Size"] = pagedResult.PageSize.ToString();
        httpContext.Response.Headers["X-Total-Pages"] = pagedResult.TotalPages.ToString();

        // Add HATEOAS links
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}";
        var links = new List<string>();

        // First page link
        links.Add($"<{baseUrl}?page=1&pageSize={pageSize}>; rel=\"first\"");

        // Last page link
        if (pagedResult.TotalPages > 0)
        {
            links.Add($"<{baseUrl}?page={pagedResult.TotalPages}&pageSize={pageSize}>; rel=\"last\"");
        }

        // Previous page link
        if (pagedResult.HasPreviousPage)
        {
            links.Add($"<{baseUrl}?page={pagedResult.PageNumber - 1}&pageSize={pageSize}>; rel=\"prev\"");
        }

        // Next page link
        if (pagedResult.HasNextPage)
        {
            links.Add($"<{baseUrl}?page={pagedResult.PageNumber + 1}&pageSize={pageSize}>; rel=\"next\"");
        }

        if (links.Any())
        {
            httpContext.Response.Headers["Link"] = string.Join(", ", links);
        }

        return Results.Ok(items);
    }

    private static async Task<IResult> GetConversation(
        Guid id,
        IConversationRepository repository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        logger.LogInformation("Getting conversation {ConversationId}", id);
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        // Extract userId from JWT claims - try multiple claim names
        var userIdClaim = context.User.FindFirst("sub") 
                         ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            // No userId claim found; treat as forbidden for safety
            logger.LogWarning("GetConversation: missing user id claim");
            return Results.Forbid();
        }

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("GetConversation: invalid user id claim value {UserId}", userIdClaim.Value);
            return Results.Forbid();
        }

        // Enforce ownership
        if (entity.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to access conversation {ConversationId} they do not own", userId, id);
            return Results.Forbid();
        }

        var dto = new ConversationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Messages = new List<MessageDto>()
        };
        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateConversation(
        CreateConversationRequest request,
        IValidator<CreateConversationRequest> validator,
        IConversationRepository repository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            // Convert validation errors to dictionary format expected by Results.ValidationProblem
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName ?? "General")
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray());
            
            logger.LogWarning("Validation failed for CreateConversation: {Errors}", 
                string.Join(", ", errors.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            
            return Results.ValidationProblem(errors);
        }

        logger.LogInformation("Creating conversation: {Title}", request.Title);

        // Extract userId from JWT claims - try multiple claim names
        var userIdClaim = context.User.FindFirst("sub") 
                         ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null)
        {
            // Log all claims for debugging
            var claims = string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"));
            logger.LogWarning("Could not extract userId from JWT. Available claims: {Claims}", claims);
            return Results.Problem("User ID could not be determined", statusCode: 400);
        }
        
        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("Could not parse userId as GUID: {UserId}", userIdClaim.Value);
            return Results.Problem("Invalid user ID format", statusCode: 400);
        }

        var entity = new Conversation(userId, request.Title);
        await repository.CreateAsync(entity, ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Messages = new List<MessageDto>()
        };

        return Results.Created($"/conversations/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateConversation(
        string id,
        UpdateConversationRequest request,
        IValidator<UpdateConversationRequest> validator,
        IConversationRepository repository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        // Validate id format
        if (!Guid.TryParse(id, out var conversationId))
        {
            logger.LogWarning("UpdateConversation: invalid GUID format {Id}", id);
            return Results.Problem("Invalid conversation ID format", statusCode: 400);
        }

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            // Convert validation errors to dictionary format expected by Results.ValidationProblem
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName ?? "General")
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray());
            
            logger.LogWarning("Validation failed for CreateConversation: {Errors}", 
                string.Join(", ", errors.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            
            return Results.ValidationProblem(errors);
        }

        logger.LogInformation("Updating conversation {ConversationId}: {Title}", conversationId, request.Title);
        
        var entity = await repository.GetByIdAsync(conversationId, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        // Ownership check
        var userIdClaim = context.User.FindFirst("sub") 
                         ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId) || entity.UserId != userId)
        {
            logger.LogWarning("User not authorized to update conversation {ConversationId}", conversationId);
            return Results.Forbid();
        }

        entity.UpdateTitle(request.Title);
        await repository.UpdateAsync(entity, ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteConversation(
        Guid id,
        IConversationRepository repository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting conversation {ConversationId}", id);
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        // Ownership check
        var userIdClaim = context.User.FindFirst("sub") 
                         ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId) || entity.UserId != userId)
        {
            logger.LogWarning("User not authorized to delete conversation {ConversationId}", id);
            return Results.Forbid();
        }
        await repository.DeleteAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMessages(
        Guid id,
        IConversationRepository repository,
        ChatDbContext dbContext,
        ILogger<Program> logger,
        HttpContext httpContext,
        string? cursor = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting messages for conversation {ConversationId}", id);

        // First verify conversation exists and user has access
        var conversation = await repository.GetByIdAsync(id, ct);
        if (conversation is null)
        {
            return Results.NotFound();
        }

        // Extract userId from JWT claims - try multiple claim names
        var userIdClaim = httpContext.User.FindFirst("sub") 
                         ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            logger.LogWarning("GetMessages: missing user id claim");
            return Results.Forbid();
        }

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("GetMessages: invalid user id claim value {UserId}", userIdClaim.Value);
            return Results.Forbid();
        }

        // Enforce ownership
        if (conversation.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to access messages for conversation {ConversationId} they do not own", userId, id);
            return Results.Forbid();
        }

        // Query messages from database
        IQueryable<Message> messagesQuery = dbContext.Messages
            .Where(m => m.ConversationId == id);

        // Apply cursor-based pagination if cursor is provided
        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorGuid))
        {
            // Get the message at the cursor position
            var cursorMessage = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == cursorGuid && m.ConversationId == id, ct);
            
            if (cursorMessage != null)
            {
                // Continue from after the cursor message
                messagesQuery = messagesQuery.Where(m => m.SentAt > cursorMessage.SentAt);
            }
        }

        // Order by sent time ascending (oldest first) - apply order after filtering
        messagesQuery = messagesQuery.OrderBy(m => m.SentAt);

        // Apply limit (max 100)
        var maxLimit = Math.Min(limit, 100);
        var messages = await messagesQuery
            .Take(maxLimit)
            .ToListAsync(ct);

        // Convert to DTOs
        var messageDtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString(),
            Content = m.Content,
            SentAt = m.SentAt,
            Attachments = new List<AttachmentDto>() // TODO: Load attachments if needed
        }).ToList();

        // Determine next cursor (ID of the last message if there might be more)
        string? nextCursor = null;
        if (messages.Count == maxLimit)
        {
            var lastMessage = messages.Last();
            nextCursor = lastMessage.Id.ToString();
        }

        var response = new PagedMessagesResponse
        {
            Items = messageDtos,
            NextCursor = nextCursor
        };

        return Results.Ok(response);
    }
}

// DTOs

/// <summary>
/// Conversation data transfer object
/// </summary>
public record ConversationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<MessageDto> Messages { get; init; } = new();
}

/// <summary>
/// Request to create a new conversation
/// </summary>
/// <param name="Title">Conversation title (1-200 characters)</param>
public record CreateConversationRequest(string Title);

/// <summary>
/// Request to update an existing conversation
/// </summary>
/// <param name="Title">New conversation title (1-200 characters)</param>
public record UpdateConversationRequest(string Title);

/// <summary>
/// Request to create a message in a conversation
/// </summary>
/// <param name="ConversationId">The conversation identifier</param>
/// <param name="Content">Message content (1-10,000 characters)</param>
public record CreateMessageRequest(Guid ConversationId, string Content);

/// <summary>
/// Message data transfer object (minimal shape to satisfy API contract)
/// </summary>
public record MessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
    public List<AttachmentDto> Attachments { get; init; } = new();
}

/// <summary>
/// Paginated response for messages with cursor-based pagination
/// </summary>
public record PagedMessagesResponse
{
    public List<MessageDto> Items { get; init; } = new();
    public string? NextCursor { get; init; }
}


using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodingAgent.Services.Chat.Api.Extensions;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;

namespace CodingAgent.Services.Chat.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/attachments")
            .WithTags("Attachments")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("", UploadAttachment)
            .WithName("UploadAttachment")
            .Produces<AttachmentDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status413PayloadTooLarge)
            .DisableAntiforgery(); // Required for file uploads

        group.MapGet("{id:guid}", GetAttachment)
            .WithName("GetAttachment")
            .Produces<AttachmentDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{id:guid}", DeleteAttachment)
            .WithName("DeleteAttachment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> UploadAttachment(
        HttpRequest request,
        IFileStorageService fileStorage,
        ILogger<Program> logger,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("UploadAttachment");
        
        try
        {
            var userId = user.GetUserId();
            activity?.SetTag("user.id", userId);

            // Check if request has files
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest("No files provided");
            }

            var file = request.Form.Files[0];
            if (file.Length == 0)
            {
                return Results.BadRequest("Empty file provided");
            }

            activity?.SetTag("file.name", file.FileName);
            activity?.SetTag("file.size", file.Length);
            activity?.SetTag("file.contentType", file.ContentType);

            // Validate file
            var validation = await fileStorage.ValidateFileAsync(
                file.FileName, 
                file.ContentType, 
                file.Length);
            
            if (!validation.IsValid)
            {
                logger.LogWarning("File validation failed for {FileName}: {Error}", 
                    file.FileName, validation.ErrorMessage);
                return Results.BadRequest(validation.ErrorMessage);
            }

            // Upload file
            string storageUrl;
            using (var stream = file.OpenReadStream())
            {
                storageUrl = await fileStorage.UploadFileAsync(
                    file.FileName,
                    file.ContentType,
                    stream,
                    userId,
                    ct);
            }

            // Generate thumbnail for images/videos
            var thumbnailUrl = await fileStorage.GenerateThumbnailAsync(
                storageUrl, 
                file.ContentType, 
                ct);

            // Create attachment entity (simplified - normally would be associated with a message)
            var attachment = new Attachment(
                messageId: Guid.Empty, // TODO: Get from request or create message
                fileName: file.FileName,
                contentType: file.ContentType,
                fileSizeBytes: file.Length,
                storageUrl: storageUrl,
                uploadedByUserId: userId);
            
            if (thumbnailUrl != null)
            {
                attachment.SetThumbnail(thumbnailUrl);
            }

            var attachmentDto = new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSizeBytes = attachment.FileSizeBytes,
                StorageUrl = storageUrl,
                ThumbnailUrl = thumbnailUrl,
                UploadedAt = attachment.UploadedAt,
                IsImage = attachment.IsImage(),
                IsVideo = attachment.IsVideo(),
                IsDocument = attachment.IsDocument()
            };

            logger.LogInformation("User {UserId} uploaded file {FileName} ({FileSize} bytes)",
                userId, file.FileName, file.Length);

            return Results.Created($"/attachments/{attachment.Id}", attachmentDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload attachment");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem("Failed to upload file");
        }
    }

    private static Task<IResult> GetAttachment(
        Guid id,
        IFileStorageService fileStorage,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetAttachment");
        activity?.SetTag("attachment.id", id);

        // TODO: In real implementation, would query database for attachment metadata
        // For now, return placeholder response
        
        return Task.FromResult(Results.NotFound($"Attachment {id} not found"));
    }

    private static Task<IResult> DeleteAttachment(
        Guid id,
        IFileStorageService fileStorage,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("DeleteAttachment");
        activity?.SetTag("attachment.id", id);
        
        var userId = user.GetUserId();

        // TODO: In real implementation, would:
        // 1. Query database for attachment
        // 2. Check if user owns the attachment or has permission
        // 3. Delete from storage
        // 4. Delete from database
        
        return Task.FromResult(Results.NotFound($"Attachment {id} not found"));
    }
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsImage { get; set; }
    public bool IsVideo { get; set; }
    public bool IsDocument { get; set; }
}
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodingAgent.Services.Chat.Api.Extensions;
using CodingAgent.Services.Chat.Domain.Services;

namespace CodingAgent.Services.Chat.Api.Endpoints;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/files")
            .WithTags("Files")
            .WithOpenApi();

        // Serve static files with security checks
        group.MapGet("{userId:guid}/{fileName}", ServeFile)
            .WithName("ServeFile")
            .Produces<FileResult>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("thumbnails/{userId:guid}/{fileName}", ServeThumbnail)
            .WithName("ServeThumbnail")
            .Produces<FileResult>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static Task<IResult> ServeFile(
        Guid userId,
        string fileName,
        HttpContext context,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            // Validate file name to prevent directory traversal
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return Task.FromResult(Results.BadRequest("Invalid file name"));
            }

            var storageBasePath = configuration["FileStorage:BasePath"] ?? 
                Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            
            var filePath = Path.Combine(storageBasePath, userId.ToString(), fileName);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(Results.NotFound());
            }

            // Get MIME type
            var contentType = GetContentType(fileName);
            
            // Set security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
            // For images/videos, allow inline display; for others, force download
            var disposition = contentType.StartsWith("image/") || contentType.StartsWith("video/") 
                ? "inline" 
                : "attachment";
            context.Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{fileName}\"";

            logger.LogInformation("Serving file {FileName} for user {UserId}", fileName, userId);

            return Task.FromResult(Results.File(filePath, contentType));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serve file {FileName} for user {UserId}", fileName, userId);
            return Task.FromResult(Results.Problem("Failed to serve file"));
        }
    }

    private static Task<IResult> ServeThumbnail(
        Guid userId,
        string fileName,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            var storageBasePath = configuration["FileStorage:BasePath"] ?? 
                Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            
            var thumbnailPath = Path.Combine(storageBasePath, "thumbnails", userId.ToString(), fileName);

            if (!File.Exists(thumbnailPath))
            {
                return Task.FromResult(Results.NotFound());
            }

            var contentType = GetContentType(fileName);
            
            logger.LogInformation("Serving thumbnail {FileName} for user {UserId}", fileName, userId);

            return Task.FromResult(Results.File(thumbnailPath, contentType));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serve thumbnail {FileName} for user {UserId}", fileName, userId);
            return Task.FromResult(Results.NotFound());
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            ".yaml" or ".yml" => "application/x-yaml",
            ".zip" => "application/zip",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            _ => "application/octet-stream"
        };
    }
}
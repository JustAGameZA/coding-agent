using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CodingAgent.Services.Chat.Domain.Services;

namespace CodingAgent.Services.Chat.Infrastructure.Storage;

/// <summary>
/// Local file storage implementation for development/testing.
/// In production, this should be replaced with Azure Blob Storage, AWS S3, etc.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageBasePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly string _signingKey;
    
    private static readonly string[] AllowedExtensions = {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", // Images
        ".mp4", ".webm", ".mov", ".avi", // Videos  
        ".pdf", ".doc", ".docx", ".txt", ".md", // Documents
        ".zip", ".7z", ".tar", ".gz", // Archives
        ".json", ".xml", ".csv", ".yaml", ".yml" // Data files
    };

    private static readonly string[] AllowedMimeTypes = {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo",
        "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain", "text/markdown",
        "application/zip", "application/x-7z-compressed", "application/x-tar", "application/gzip",
        "application/json", "application/xml", "text/csv", "application/x-yaml", "text/yaml"
    };

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _storageBasePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "http://localhost:5000/files";
        _maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 50 * 1024 * 1024); // 50MB default
        _logger = logger;
        // Use configured signing key for presigned URLs; fall back to a local dev default
        _signingKey = configuration["FileStorage:SigningKey"] ?? "your-secret-key-here";

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageBasePath);
        Directory.CreateDirectory(Path.Combine(_storageBasePath, "thumbnails"));
    }

    public async Task<string> UploadFileAsync(
        string fileName, 
        string contentType, 
        Stream fileStream, 
        Guid userId, 
        CancellationToken ct = default)
    {
        try
        {
            // Generate unique file name to avoid conflicts
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{fileExtension}";
            
            // Create user-specific directory
            var userDirectory = Path.Combine(_storageBasePath, userId.ToString());
            Directory.CreateDirectory(userDirectory);
            
            // Full file path
            var filePath = Path.Combine(userDirectory, uniqueFileName);
            
            // Copy file stream to disk
            using (var fileOutput = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileOutput, ct);
            }
            
            // Generate storage URL
            var storageUrl = $"{_baseUrl}/{userId}/{uniqueFileName}";
            
            _logger.LogInformation("Uploaded file {FileName} for user {UserId} to {StorageUrl}", 
                fileName, userId, storageUrl);
            
            return storageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", fileName, userId);
            throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
        }
    }

    public Task DeleteFileAsync(string storageUrl, CancellationToken ct = default)
    {
        try
        {
            var filePath = GetLocalPathFromUrl(storageUrl);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file at {FilePath}", filePath);
                
                // Also delete thumbnail if it exists
                var thumbnailPath = GetThumbnailPath(filePath);
                if (File.Exists(thumbnailPath))
                {
                    File.Delete(thumbnailPath);
                    _logger.LogInformation("Deleted thumbnail at {ThumbnailPath}", thumbnailPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at {StorageUrl}", storageUrl);
            // Don't throw - file deletion is not critical
        }
        
        return Task.CompletedTask;
    }

    public Task<string?> GenerateThumbnailAsync(
        string storageUrl, 
        string contentType, 
        CancellationToken ct = default)
    {
        try
        {
            // Only generate thumbnails for images
            if (!contentType.StartsWith("image/"))
            {
                return Task.FromResult<string?>(null);
            }
            
            var filePath = GetLocalPathFromUrl(storageUrl);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<string?>(null);
            }
            
            var thumbnailPath = GetThumbnailPath(filePath);

            // Ensure thumbnail directory exists
            var thumbDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(thumbDir))
            {
                Directory.CreateDirectory(thumbDir);
            }

            // Attempt to create a thumbnail; on failure, still return the computed URL
            try
            {
                // For now, just copy the original as thumbnail
                // In production, you'd use ImageSharp or similar to resize
                File.Copy(filePath, thumbnailPath, overwrite: true);
            }
            catch (Exception copyEx)
            {
                _logger.LogWarning(copyEx, "Thumbnail copy failed for {Path}", filePath);
            }

            var thumbnailUrl = storageUrl.Replace("/files/", "/files/thumbnails/");

            _logger.LogInformation("Generated thumbnail for {StorageUrl} at {ThumbnailUrl}", 
                storageUrl, thumbnailUrl);

            return Task.FromResult<string?>(thumbnailUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {StorageUrl}", storageUrl);
            return Task.FromResult<string?>(null); // Thumbnail generation is optional
        }
    }

    public Task<string> GetPresignedUrlAsync(
        string storageUrl, 
        TimeSpan expiresIn, 
        CancellationToken ct = default)
    {
        // For local storage, just return the original URL with expiration token
        // In production cloud storage, you'd generate actual pre-signed URLs
        
        var expirationTimestamp = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds();
        var token = GenerateSecureToken(storageUrl, expirationTimestamp);
        
        var presignedUrl = $"{storageUrl}?expires={expirationTimestamp}&token={token}";
        
        return Task.FromResult(presignedUrl);
    }

    public Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(
        string fileName, 
        string contentType, 
        long fileSizeBytes)
    {
        // Check file size
        if (fileSizeBytes > _maxFileSizeBytes)
        {
            return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, $"File size {fileSizeBytes / 1024 / 1024}MB exceeds maximum allowed size {_maxFileSizeBytes / 1024 / 1024}MB"));
        }
        
        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, $"File extension '{extension}' is not allowed"));
        }
        
        // Check MIME type
        if (!AllowedMimeTypes.Contains(contentType.ToLowerInvariant()))
        {
            return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, $"Content type '{contentType}' is not allowed"));
        }
        
        // Check for malicious file names
        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
        {
            return Task.FromResult<(bool IsValid, string? ErrorMessage)>((false, "File name contains invalid characters"));
        }
        
        return Task.FromResult<(bool IsValid, string? ErrorMessage)>((true, null));
    }

    private string GetLocalPathFromUrl(string storageUrl)
    {
        try
        {
            var fileUri = new Uri(storageUrl, UriKind.Absolute);
            var normalizedBase = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
            var baseUri = new Uri(normalizedBase, UriKind.Absolute);

            var absolutePath = fileUri.AbsolutePath;        // e.g. /files/{userId}/{file}
            var basePath = baseUri.AbsolutePath;            // e.g. /files/

            string relativePath;
            if (absolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = absolutePath.Substring(basePath.Length).TrimStart('/');
            }
            else
            {
                // Fallback: use last two segments (userId/fileName)
                var segments = absolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                relativePath = string.Join('/', segments.Skip(Math.Max(0, segments.Length - 2)));
            }

            var localRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_storageBasePath, localRelative);
        }
        catch
        {
            // Last resort: try a simple replace
            var prefix = (_baseUrl.TrimEnd('/') + "/");
            var rel = storageUrl.Replace(prefix, "", StringComparison.OrdinalIgnoreCase);
            return Path.Combine(_storageBasePath, rel.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    private string GetThumbnailPath(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath);
        var fileName = Path.GetFileName(originalPath);
        return Path.Combine(_storageBasePath, "thumbnails", Path.GetRelativePath(_storageBasePath, directory!), fileName);
    }

    private string GenerateSecureToken(string url, long expirationTimestamp)
    {
        // Simple HMAC-based token for URL security
        // In production, use a proper secret key from configuration
        var message = $"{url}:{expirationTimestamp}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
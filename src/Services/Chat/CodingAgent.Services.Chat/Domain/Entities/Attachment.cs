namespace CodingAgent.Services.Chat.Domain.Entities;

public class Attachment
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string StorageUrl { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }

    // Navigation properties
    public Message Message { get; private set; } = null!;

    // EF Core constructor
    private Attachment() { }

    public Attachment(
        Guid messageId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storageUrl,
        Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        StorageUrl = storageUrl;
        UploadedAt = DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
    }

    public void SetThumbnail(string thumbnailUrl)
    {
        ThumbnailUrl = thumbnailUrl;
    }

    public bool IsImage() => ContentType.StartsWith("image/");
    public bool IsVideo() => ContentType.StartsWith("video/");
    public bool IsDocument() => ContentType.StartsWith("application/") || ContentType.StartsWith("text/");

    public string GetFileExtension()
    {
        return Path.GetExtension(FileName).ToLowerInvariant();
    }

    public bool IsAllowedFileType()
    {
        var allowedExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", // Images
            ".mp4", ".webm", ".mov", ".avi", // Videos
            ".pdf", ".doc", ".docx", ".txt", ".md", // Documents
            ".zip", ".7z", ".tar", ".gz", // Archives
            ".json", ".xml", ".csv", ".yaml", ".yml" // Data files
        };

        return allowedExtensions.Contains(GetFileExtension());
    }

    public bool IsWithinSizeLimit(long maxSizeBytes = 50 * 1024 * 1024) // 50MB default
    {
        return FileSizeBytes <= maxSizeBytes;
    }
}
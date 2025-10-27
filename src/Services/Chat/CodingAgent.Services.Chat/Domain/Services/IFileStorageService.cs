namespace CodingAgent.Services.Chat.Domain.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage and return the storage URL
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="userId">User uploading the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Storage URL where file can be accessed</returns>
    Task<string> UploadFileAsync(
        string fileName, 
        string contentType, 
        Stream fileStream, 
        Guid userId, 
        CancellationToken ct = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="storageUrl">URL of the file to delete</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteFileAsync(string storageUrl, CancellationToken ct = default);

    /// <summary>
    /// Generate a thumbnail for image/video files
    /// </summary>
    /// <param name="storageUrl">Original file storage URL</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Thumbnail storage URL or null if not applicable</returns>
    Task<string?> GenerateThumbnailAsync(
        string storageUrl, 
        string contentType, 
        CancellationToken ct = default);

    /// <summary>
    /// Get a pre-signed URL for secure file access
    /// </summary>
    /// <param name="storageUrl">Storage URL</param>
    /// <param name="expiresIn">URL expiration time</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pre-signed URL for temporary access</returns>
    Task<string> GetPresignedUrlAsync(
        string storageUrl, 
        TimeSpan expiresIn, 
        CancellationToken ct = default);

    /// <summary>
    /// Validate file before upload
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <returns>Validation result with error message if invalid</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(
        string fileName, 
        string contentType, 
        long fileSizeBytes);
}
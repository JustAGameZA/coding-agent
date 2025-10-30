using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Text;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodingAgent.Services.Chat.Tests.Unit.Infrastructure.Storage;

[Trait("Category", "Unit")]
public class LocalFileStorageServiceTests
{
    private readonly LocalFileStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<LocalFileStorageService>> _loggerMock;
    private readonly string _testDirectory;

    public LocalFileStorageServiceTests()
    {
        var initialData = new Dictionary<string, string?>();
        _loggerMock = new Mock<ILogger<LocalFileStorageService>>();
        
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "chat_test_uploads", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        // Build in-memory configuration (avoids mocking extension methods like GetValue<T>)
        initialData["FileStorage:BasePath"] = _testDirectory;
        initialData["FileStorage:BaseUrl"] = "http://localhost:5000/files";
        initialData["FileStorage:MaxFileSizeBytes"] = (50 * 1024 * 1024).ToString(); // 50MB
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        _storageService = new LocalFileStorageService(_configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var fileSizeBytes = 1024 * 1024; // 1MB

        // Act
        var result = await _storageService.ValidateFileAsync(fileName, contentType, fileSizeBytes);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateFileAsync_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var fileSizeBytes = 100 * 1024 * 1024; // 100MB (exceeds 50MB limit)

        // Act
        var result = await _storageService.ValidateFileAsync(fileName, contentType, fileSizeBytes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum allowed size");
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse()
    {
        // Arrange
        var fileName = "test.exe";
        var contentType = "application/octet-stream";
        var fileSizeBytes = 1024;

        // Act
        var result = await _storageService.ValidateFileAsync(fileName, contentType, fileSizeBytes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not allowed");
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidMimeType_ReturnsFalse()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "application/x-malware";
        var fileSizeBytes = 1024;

        // Act
        var result = await _storageService.ValidateFileAsync(fileName, contentType, fileSizeBytes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not allowed");
    }

    [Fact]
    public async Task ValidateFileAsync_MaliciousFileName_ReturnsFalse()
    {
        // Arrange
        var fileName = "../../../malicious.jpg";
        var contentType = "image/jpeg";
        var fileSizeBytes = 1024;

        // Act
        var result = await _storageService.ValidateFileAsync(fileName, contentType, fileSizeBytes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("invalid characters");
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsStorageUrl()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var userId = Guid.NewGuid();
        var fileContent = "Test file content";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        // Act
        var storageUrl = await _storageService.UploadFileAsync(fileName, contentType, stream, userId);

        // Assert
        storageUrl.Should().StartWith("http://localhost:5000/files/");
        storageUrl.Should().Contain(userId.ToString());
        storageUrl.Should().EndWith(".txt");

        // Verify file was actually created
        var userDirectory = Path.Combine(_testDirectory, userId.ToString());
        Directory.Exists(userDirectory).Should().BeTrue();
        Directory.GetFiles(userDirectory).Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_ImageFile_ReturnsThumbnailUrl()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var userId = Guid.NewGuid();
        var fileContent = new byte[] { 0xFF, 0xD8, 0xFF }; // Minimal JPEG header
        using var stream = new MemoryStream(fileContent);

        // First upload the file
        var storageUrl = await _storageService.UploadFileAsync(fileName, contentType, stream, userId);

        // Act
        var thumbnailUrl = await _storageService.GenerateThumbnailAsync(storageUrl, contentType);

        // Assert
        thumbnailUrl.Should().NotBeNull();
        thumbnailUrl.Should().Contain("thumbnails");
    }

    [Fact]
    public async Task GenerateThumbnailAsync_NonImageFile_ReturnsNull()
    {
        // Arrange
        var storageUrl = "http://localhost:5000/files/test/document.pdf";
        var contentType = "application/pdf";

        // Act
        var thumbnailUrl = await _storageService.GenerateThumbnailAsync(storageUrl, contentType);

        // Assert
        thumbnailUrl.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesFile()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var userId = Guid.NewGuid();
        var fileContent = "Test file content";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        // Upload file first
        var storageUrl = await _storageService.UploadFileAsync(fileName, contentType, stream, userId);

        // Act
        await _storageService.DeleteFileAsync(storageUrl);

        // Assert
        var userDirectory = Path.Combine(_testDirectory, userId.ToString());
        Directory.GetFiles(userDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ValidUrl_ReturnsSignedUrl()
    {
        // Arrange
        var storageUrl = "http://localhost:5000/files/test/file.txt";
        var expiresIn = TimeSpan.FromHours(1);

        // Act
        var presignedUrl = await _storageService.GetPresignedUrlAsync(storageUrl, expiresIn);

        // Assert
        presignedUrl.Should().StartWith(storageUrl);
        presignedUrl.Should().Contain("expires=");
        presignedUrl.Should().Contain("token=");
    }

    private void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}

[Trait("Category", "Unit")]
public class AttachmentTests
{
    [Fact]
    public void Attachment_ValidConstruction_SetsProperties()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var fileSizeBytes = 1024;
        var storageUrl = "http://localhost/files/test.jpg";
        var userId = Guid.NewGuid();

        // Act
        var attachment = new Attachment(messageId, fileName, contentType, fileSizeBytes, storageUrl, userId);

        // Assert
        attachment.Id.Should().NotBeEmpty();
        attachment.MessageId.Should().Be(messageId);
        attachment.FileName.Should().Be(fileName);
        attachment.ContentType.Should().Be(contentType);
        attachment.FileSizeBytes.Should().Be(fileSizeBytes);
        attachment.StorageUrl.Should().Be(storageUrl);
        attachment.UploadedByUserId.Should().Be(userId);
        attachment.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("application/pdf", false)]
    [InlineData("text/plain", false)]
    public void IsImage_VariousContentTypes_ReturnsExpected(string contentType, bool expected)
    {
        // Arrange
        var attachment = CreateTestAttachment(contentType: contentType);

        // Act & Assert
        attachment.IsImage().Should().Be(expected);
    }

    [Theory]
    [InlineData("video/mp4", true)]
    [InlineData("video/webm", true)]
    [InlineData("image/jpeg", false)]
    [InlineData("application/pdf", false)]
    public void IsVideo_VariousContentTypes_ReturnsExpected(string contentType, bool expected)
    {
        // Arrange
        var attachment = CreateTestAttachment(contentType: contentType);

        // Act & Assert
        attachment.IsVideo().Should().Be(expected);
    }

    [Theory]
    [InlineData("test.jpg", ".jpg")]
    [InlineData("document.PDF", ".pdf")]
    [InlineData("NoExtension", "")]
    public void GetFileExtension_VariousFileNames_ReturnsLowerCaseExtension(string fileName, string expected)
    {
        // Arrange
        var attachment = CreateTestAttachment(fileName: fileName);

        // Act & Assert
        attachment.GetFileExtension().Should().Be(expected);
    }

    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("test.exe", false)]
    [InlineData("test.pdf", true)]
    [InlineData("test.mp4", true)]
    public void IsAllowedFileType_VariousExtensions_ReturnsExpected(string fileName, bool expected)
    {
        // Arrange
        var attachment = CreateTestAttachment(fileName: fileName);

        // Act & Assert
        attachment.IsAllowedFileType().Should().Be(expected);
    }

    [Theory]
    [InlineData(1024, true)] // 1KB
    [InlineData(50 * 1024 * 1024, true)] // 50MB (at limit)
    [InlineData(100 * 1024 * 1024, false)] // 100MB (exceeds limit)
    public void IsWithinSizeLimit_VariousSizes_ReturnsExpected(long fileSizeBytes, bool expected)
    {
        // Arrange
        var attachment = CreateTestAttachment(fileSizeBytes: fileSizeBytes);

        // Act & Assert
        attachment.IsWithinSizeLimit().Should().Be(expected);
    }

    [Fact]
    public void SetThumbnail_ValidUrl_SetsThumbnailUrl()
    {
        // Arrange
        var attachment = CreateTestAttachment();
        var thumbnailUrl = "http://localhost/thumbnails/test.jpg";

        // Act
        attachment.SetThumbnail(thumbnailUrl);

        // Assert
        attachment.ThumbnailUrl.Should().Be(thumbnailUrl);
    }

    private static Attachment CreateTestAttachment(
        string fileName = "test.jpg",
        string contentType = "image/jpeg",
        long fileSizeBytes = 1024)
    {
        return new Attachment(
            messageId: Guid.NewGuid(),
            fileName: fileName,
            contentType: contentType,
            fileSizeBytes: fileSizeBytes,
            storageUrl: "http://localhost/files/test.jpg",
            uploadedByUserId: Guid.NewGuid());
    }
}
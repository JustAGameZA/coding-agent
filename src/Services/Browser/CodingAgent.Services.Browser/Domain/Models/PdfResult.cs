namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Result of PDF generation
/// </summary>
public record PdfResult
{
    /// <summary>
    /// Base64 encoded PDF data
    /// </summary>
    public required string PdfData { get; init; }

    /// <summary>
    /// The final URL after any redirects
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Time taken in milliseconds
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Size of the PDF in bytes
    /// </summary>
    public long SizeBytes { get; init; }
}

namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Request model for PDF generation
/// </summary>
public record PdfRequest
{
    /// <summary>
    /// The URL to navigate to
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Timeout in milliseconds (default: 30000)
    /// </summary>
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Page format (e.g., A4, Letter)
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Page width (e.g., "210mm")
    /// </summary>
    public string? Width { get; init; }

    /// <summary>
    /// Page height (e.g., "297mm")
    /// </summary>
    public string? Height { get; init; }

    /// <summary>
    /// Margin top (e.g., "10mm")
    /// </summary>
    public string? MarginTop { get; init; }

    /// <summary>
    /// Margin right (e.g., "10mm")
    /// </summary>
    public string? MarginRight { get; init; }

    /// <summary>
    /// Margin bottom (e.g., "10mm")
    /// </summary>
    public string? MarginBottom { get; init; }

    /// <summary>
    /// Margin left (e.g., "10mm")
    /// </summary>
    public string? MarginLeft { get; init; }

    /// <summary>
    /// Whether to print background graphics
    /// </summary>
    public bool PrintBackground { get; init; } = true;
}

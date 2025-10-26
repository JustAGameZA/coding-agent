using CodingAgent.Services.Browser.Domain.Models;
using FluentValidation;

namespace CodingAgent.Services.Browser.Api.Validators;

/// <summary>
/// Validator for PDF generation requests
/// </summary>
public class PdfRequestValidator : AbstractValidator<PdfRequest>
{
    public PdfRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("URL is required")
            .Must(BeAValidUrl)
            .WithMessage("URL must be a valid HTTP or HTTPS URL");

        RuleFor(x => x.TimeoutMs)
            .GreaterThan(0)
            .When(x => x.TimeoutMs.HasValue)
            .WithMessage("Timeout must be greater than 0");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

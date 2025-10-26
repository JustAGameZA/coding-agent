using CodingAgent.Services.Browser.Domain.Models;
using FluentValidation;

namespace CodingAgent.Services.Browser.Api.Validators;

/// <summary>
/// Validator for browse requests
/// </summary>
public class BrowseRequestValidator : AbstractValidator<BrowseRequest>
{
    public BrowseRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("URL is required")
            .Must(BeAValidUrl)
            .WithMessage("URL must be a valid HTTP or HTTPS URL");

        RuleFor(x => x.BrowserType)
            .NotEmpty()
            .Must(BeAValidBrowserType)
            .WithMessage("Browser type must be 'chromium' or 'firefox'");

        RuleFor(x => x.TimeoutMs)
            .GreaterThan(0)
            .LessThanOrEqualTo(120000)
            .When(x => x.TimeoutMs.HasValue)
            .WithMessage("Timeout must be between 1 and 120000 milliseconds");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeAValidBrowserType(string browserType)
    {
        return browserType.ToLowerInvariant() is "chromium" or "firefox";
    }
}

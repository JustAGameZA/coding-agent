using CodingAgent.Services.Browser.Domain.Models;
using FluentValidation;

namespace CodingAgent.Services.Browser.Api.Validators;

/// <summary>
/// Validator for screenshot requests
/// </summary>
public class ScreenshotRequestValidator : AbstractValidator<ScreenshotRequest>
{
    public ScreenshotRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("URL is required")
            .Must(BeAValidUrl)
            .WithMessage("URL must be a valid HTTP or HTTPS URL");

        RuleFor(x => x.BrowserType)
            .NotEmpty()
            .WithMessage("Browser type is required")
            .Must(x => x == "chromium" || x == "firefox")
            .WithMessage("Browser type must be 'chromium' or 'firefox'");

        RuleFor(x => x.TimeoutMs)
            .GreaterThan(0)
            .When(x => x.TimeoutMs.HasValue)
            .WithMessage("Timeout must be greater than 0");

        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("Format is required")
            .Must(x => x.ToLowerInvariant() == "png" || x.ToLowerInvariant() == "jpeg")
            .WithMessage("Format must be 'png' or 'jpeg'");

        RuleFor(x => x.Quality)
            .InclusiveBetween(0, 100)
            .When(x => x.Quality.HasValue)
            .WithMessage("Quality must be between 0 and 100");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

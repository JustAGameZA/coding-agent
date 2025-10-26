using System.Diagnostics;
using CodingAgent.Services.Browser.Domain.Models;
using CodingAgent.Services.Browser.Domain.Services;
using FluentValidation;

namespace CodingAgent.Services.Browser.Api.Endpoints;

/// <summary>
/// Endpoints for browser automation
/// </summary>
public static class BrowserEndpoints
{
    public static void MapBrowserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/browse")
            .WithTags("Browser");

        group.MapPost("", Browse)
            .WithName("Browse")
            .WithDescription("Navigate to a URL and retrieve page content. Supports Chromium and Firefox browsers.")
            .WithSummary("Browse a URL")
            .Produces<BrowseResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status408RequestTimeout)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/screenshot", CaptureScreenshot)
            .WithName("CaptureScreenshot")
            .WithDescription("Capture a screenshot of a page or element. Supports full page screenshots and element-specific screenshots.")
            .WithSummary("Capture screenshot")
            .Produces<ScreenshotResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status408RequestTimeout)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/extract", ExtractContent)
            .WithName("ExtractContent")
            .WithDescription("Extract content (text, links, images) from a page.")
            .WithSummary("Extract page content")
            .Produces<ExtractedContent>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status408RequestTimeout)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/interact", InteractWithForm)
            .WithName("InteractWithForm")
            .WithDescription("Interact with a form by filling fields and optionally submitting.")
            .WithSummary("Interact with form")
            .Produces<FormInteractionResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status408RequestTimeout)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/pdf", GeneratePdf)
            .WithName("GeneratePdf")
            .WithDescription("Generate a PDF from a page. Only works with Chromium browser.")
            .WithSummary("Generate PDF")
            .Produces<PdfResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status408RequestTimeout)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Browse(
        BrowseRequest request,
        IBrowserService browserService,
        IValidator<BrowseRequest> validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("BrowseEndpoint");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);

        // Validate request
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid browse request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await browserService.BrowseAsync(request, cancellationToken);
            
            logger.LogInformation("Browse request completed for {Url} in {LoadTimeMs}ms",
                result.Url, result.LoadTimeMs);

            return Results.Ok(result);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            return Results.Problem(
                title: "Request Timeout",
                detail: $"The request to {request.Url} timed out",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing browse request for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem(
                title: "Browse Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> CaptureScreenshot(
        ScreenshotRequest request,
        IBrowserService browserService,
        IValidator<ScreenshotRequest> validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("CaptureScreenshotEndpoint");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);
        activity?.SetTag("full_page", request.FullPage);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid screenshot request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await browserService.CaptureScreenshotAsync(request, cancellationToken);
            
            logger.LogInformation("Screenshot captured for {Url} in {DurationMs}ms",
                result.Url, result.DurationMs);

            return Results.Ok(result);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            return Results.Problem(
                title: "Request Timeout",
                detail: $"The request to {request.Url} timed out",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error capturing screenshot for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem(
                title: "Screenshot Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ExtractContent(
        ExtractContentRequest request,
        IBrowserService browserService,
        IValidator<ExtractContentRequest> validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("ExtractContentEndpoint");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid extract content request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await browserService.ExtractContentAsync(request, cancellationToken);
            
            logger.LogInformation("Content extracted from {Url} in {DurationMs}ms: {LinksCount} links, {ImagesCount} images",
                result.Url, result.DurationMs, result.Links.Count, result.Images.Count);

            return Results.Ok(result);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            return Results.Problem(
                title: "Request Timeout",
                detail: $"The request to {request.Url} timed out",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting content from {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem(
                title: "Content Extraction Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> InteractWithForm(
        FormInteractionRequest request,
        IBrowserService browserService,
        IValidator<FormInteractionRequest> validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("InteractWithFormEndpoint");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);
        activity?.SetTag("fields_count", request.Fields.Count);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid form interaction request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await browserService.InteractWithFormAsync(request, cancellationToken);
            
            logger.LogInformation("Form interaction completed for {Url} in {DurationMs}ms, success: {Success}",
                result.Url, result.DurationMs, result.Success);

            return Results.Ok(result);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            return Results.Problem(
                title: "Request Timeout",
                detail: $"The request to {request.Url} timed out",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error interacting with form at {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem(
                title: "Form Interaction Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GeneratePdf(
        PdfRequest request,
        IBrowserService browserService,
        IValidator<PdfRequest> validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("GeneratePdfEndpoint");
        activity?.SetTag("url", request.Url);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid PDF generation request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await browserService.GeneratePdfAsync(request, cancellationToken);
            
            logger.LogInformation("PDF generated for {Url} in {DurationMs}ms, size: {SizeBytes} bytes",
                result.Url, result.DurationMs, result.SizeBytes);

            return Results.Ok(result);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            return Results.Problem(
                title: "Request Timeout",
                detail: $"The request to {request.Url} timed out",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating PDF for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem(
                title: "PDF Generation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

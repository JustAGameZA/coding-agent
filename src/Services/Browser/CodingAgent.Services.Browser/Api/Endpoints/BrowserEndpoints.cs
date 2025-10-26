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
}

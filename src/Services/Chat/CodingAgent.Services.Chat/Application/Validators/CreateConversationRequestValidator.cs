using FluentValidation;

namespace CodingAgent.Services.Chat.Application.Validators;

/// <summary>
/// Validator for CreateConversationRequest
/// </summary>
public class CreateConversationRequestValidator : AbstractValidator<Api.Endpoints.CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Title is required")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters");
    }
}

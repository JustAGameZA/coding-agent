using FluentValidation;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// Validator for CreateTaskRequest
/// </summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .Length(1, 10000)
            .WithMessage("Description must be between 1 and 10,000 characters");
    }
}

/// <summary>
/// Validator for UpdateTaskRequest
/// </summary>
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .Length(1, 10000)
            .WithMessage("Description must be between 1 and 10,000 characters");
    }
}

/// <summary>
/// Validator for ExecuteTaskRequest
/// </summary>
public class ExecuteTaskRequestValidator : AbstractValidator<ExecuteTaskRequest>
{
    public ExecuteTaskRequestValidator()
    {
        RuleFor(x => x.Strategy)
            .IsInEnum()
            .When(x => x.Strategy.HasValue)
            .WithMessage("Invalid execution strategy specified");
    }
}

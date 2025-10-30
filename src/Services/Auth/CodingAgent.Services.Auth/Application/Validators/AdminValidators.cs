using CodingAgent.Services.Auth.Application.DTOs;
using FluentValidation;

namespace CodingAgent.Services.Auth.Application.Validators;

/// <summary>
/// Validator for updating user roles
/// </summary>
public class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesRequest>
{
    private static readonly string[] AllowedRoles = { "User", "Admin" };

    public UpdateUserRolesValidator()
    {
        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles cannot be null");

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("At least one role is required");

        RuleFor(x => x.Roles)
            .Must(roles => roles.All(r => AllowedRoles.Contains(r)))
            .WithMessage($"Invalid role. Allowed roles: {string.Join(", ", AllowedRoles)}");

        RuleFor(x => x.Roles)
            .Must(roles => roles.Distinct().Count() == roles.Length)
            .WithMessage("Duplicate roles are not allowed");
    }
}

/// <summary>
/// Validator for user list query parameters
/// </summary>
public class UserListQueryValidator : AbstractValidator<UserListQuery>
{
    public UserListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrWhiteSpace(role) || role == "User" || role == "Admin")
            .WithMessage("Role filter must be either 'User' or 'Admin'");
    }
}

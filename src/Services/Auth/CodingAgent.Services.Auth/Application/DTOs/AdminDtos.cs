namespace CodingAgent.Services.Auth.Application.DTOs;

/// <summary>
/// Response containing paginated list of users
/// </summary>
public record UserListResponse(
    List<UserSummaryDto> Users,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// Summary view of a user for list display
/// </summary>
public record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string[] Roles,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>
/// Detailed view of a user including session count
/// </summary>
public record UserDetailDto(
    Guid Id,
    string Username,
    string Email,
    string[] Roles,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int SessionCount
);

/// <summary>
/// Request to update user roles
/// </summary>
public record UpdateUserRolesRequest(
    string[] Roles
);

/// <summary>
/// Query parameters for user list pagination and filtering
/// </summary>
public record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null
);

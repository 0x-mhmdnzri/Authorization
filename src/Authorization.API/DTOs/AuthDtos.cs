using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? ClearanceLevel { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiration { get; set; }

    /// <summary>Backward-compatible alias for AccessToken. </summary>
    public string Token => AccessToken;
    public DateTime Expiration => AccessTokenExpiration;

    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsGod { get; set; }
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AssignRoleRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = string.Empty;
}

public class CreateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class CreateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? ClearanceLevel { get; set; }
    public List<string>? Roles { get; set; }
}

public class AssignSectionPermissionRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string SectionKey { get; set; } = string.Empty;

    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class MenuItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Href { get; set; }
    public string? Icon { get; set; }
    public string AuthorizationMethods { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public List<MenuItemDto> Children { get; set; } = new();
}

using Microsoft.AspNetCore.Identity;

namespace Authorization.API.Models;

/// <summary>
/// Custom user entity extending IdentityUser.
/// Identity tables are used as-is with custom properties.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? ClearanceLevel { get; set; } // Useful for MAC / ABAC later
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation for RBAC (roles come from IdentityRole via UserRoles)
    // Additional custom relations can be added later for ReBAC etc.
}

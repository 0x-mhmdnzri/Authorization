using Microsoft.AspNetCore.Identity;

namespace Authorization.API.Models;

/// <summary>
/// Custom role entity extending IdentityRole.
/// Used for RBAC baseline.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // For hierarchical RBAC later
    public string? ParentRoleId { get; set; }
    public ApplicationRole? ParentRole { get; set; }
}

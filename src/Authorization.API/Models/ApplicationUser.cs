using Microsoft.AspNetCore.Identity;

namespace Authorization.API.Models;

/// <summary>
/// Custom user entity extending IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? ClearanceLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Single GOD user flag. Only one user in the system should have IsGod = true.
    /// GOD bypasses all section permission checks and can manage every lower level.
    /// </summary>
    public bool IsGod { get; set; } = false;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<SectionPermission> SectionPermissions { get; set; } = new List<SectionPermission>();
}

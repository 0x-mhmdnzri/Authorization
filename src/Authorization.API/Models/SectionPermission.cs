namespace Authorization.API.Models;

/// <summary>
/// Per-user read/write access to a menu section.
/// Assigned by GOD or a higher-level manager (DAC + RBAC hybrid).
/// </summary>
public class SectionPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MenuSectionId { get; set; }
    public MenuSection? MenuSection { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }

    public string GrantedById { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive =>
        (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}

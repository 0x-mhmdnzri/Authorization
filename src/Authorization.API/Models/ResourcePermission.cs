namespace Authorization.API.Models;

/// <summary>
/// Discretionary Access Control (DAC) entry.
/// The resource owner decides who receives which permissions.
/// </summary>
public class ResourcePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    /// <summary>
    /// User who is granted the permission (ApplicationUser.Id)
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated or flags: Read, Write, Delete, Share
    /// </summary>
    public string Permissions { get; set; } = "Read";

    public string GrantedById { get; set; } = string.Empty; // owner or someone with Share
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

namespace Authorization.API.Models;

/// <summary>
/// Defined purpose for Purpose-Based Access Control (PBAC-Purpose).
/// Access is only allowed when the requested purpose is explicitly permitted.
/// Classic example: medical data accessible for "Treatment" but not "Research" without extra approval.
/// </summary>
public class Purpose
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;          // e.g. TREATMENT, RESEARCH, BILLING
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Which purposes are allowed on a specific resource (or resource type).
/// </summary>
public class ResourcePurpose
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public Guid PurposeId { get; set; }
    public Purpose Purpose { get; set; } = null!;
    /// <summary>Optional: only these roles may use this purpose on the resource.</summary>
    public string? AllowedRoles { get; set; }
    public bool RequiresExplicitConsent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Audit / consent record when a user accesses data for a stated purpose.
/// </summary>
public class PurposeAccessLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid PurposeId { get; set; }
    public string Action { get; set; } = "Read";
    public bool Allowed { get; set; }
    public string? Reason { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}

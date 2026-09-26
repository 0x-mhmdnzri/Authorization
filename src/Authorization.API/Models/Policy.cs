namespace Authorization.API.Models;

/// <summary>
/// Central Policy-Based Access Control (PBAC) policy.
/// Policies live outside individual applications and can combine
/// roles, attributes, relationships and context under one governance layer.
/// </summary>
public class Policy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Policy target: ResourceType or "*" for any
    /// </summary>
    public string ResourceType { get; set; } = "*";

    /// <summary>
    /// Action: Read, Write, Delete, Share, * 
    /// </summary>
    public string Action { get; set; } = "*";

    /// <summary>
    /// Effect when the policy matches: Allow or Deny
    /// </summary>
    public string Effect { get; set; } = "Allow";

    /// <summary>
    /// Higher priority evaluated first (Deny usually higher)
    /// </summary>
    public int Priority { get; set; } = 100;

    public bool IsEnabled { get; set; } = true;

    // ---- Conditions (simplified policy language) ----

    /// <summary>Required roles (comma-separated). Empty = any role.</summary>
    public string? RequiredRoles { get; set; }

    /// <summary>Required departments (comma-separated).</summary>
    public string? RequiredDepartments { get; set; }

    /// <summary>Minimum clearance level.</summary>
    public string? MinimumClearance { get; set; }

    /// <summary>Require same department as resource owner.</summary>
    public bool RequireSameDepartment { get; set; }

    /// <summary>Only during business hours (08:00-18:00 UTC).</summary>
    public bool RequireBusinessHours { get; set; }

    /// <summary>Maximum resource sensitivity allowed.</summary>
    public string? MaxResourceSensitivity { get; set; }

    /// <summary>
    /// Optional: also require a DAC ACL entry for the subject.
    /// </summary>
    public bool RequireDacGrant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

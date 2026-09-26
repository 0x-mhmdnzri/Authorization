namespace Authorization.API.Models;

/// <summary>
/// ABAC Policy definition.
/// Conditions are evaluated against Subject (User), Resource, Action and Environment attributes.
/// </summary>
public class AbacPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// Target resource type this policy applies to (null = any)
    /// </summary>
    public string? ResourceType { get; set; }
    
    /// <summary>
    /// Action this policy governs (Read, Write, Delete, Share, ...)
    /// </summary>
    public string Action { get; set; } = "Read";
    
    /// <summary>
    /// Effect when conditions match: Allow or Deny
    /// </summary>
    public string Effect { get; set; } = "Allow"; // Allow | Deny
    
    /// <summary>
    /// Priority (higher = evaluated first). Deny usually has higher priority.
    /// </summary>
    public int Priority { get; set; } = 100;
    
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Simple condition flags (can be extended later to full expression language)
    public bool RequireSameDepartment { get; set; }
    public bool RequireBusinessHours { get; set; } // 08:00-18:00 UTC
    public string? MinimumClearance { get; set; } // e.g. Confidential
    public string? AllowedDepartments { get; set; } // comma separated
    public string? RequiredSensitivityMax { get; set; } // user can access resources up to this sensitivity
}

namespace Authorization.API.Models;

/// <summary>
/// Privileged Access Control (PAC) – definition of a privileged capability.
/// Users do not hold these permanently; they request time-boxed elevation (JIT).
/// </summary>
public class PrivilegeDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;          // e.g. PROD_DB_ADMIN, K8S_ADMIN
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Default duration in minutes when approved.</summary>
    public int DefaultDurationMinutes { get; set; } = 60;
    public int MaxDurationMinutes { get; set; } = 480;
    /// <summary>Roles allowed to request this privilege.</summary>
    public string? AllowedRequesterRoles { get; set; }
    /// <summary>Roles that can approve (empty = any Admin).</summary>
    public string? ApproverRoles { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ElevationStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Active = 3,
    Expired = 4,
    Revoked = 5
}

/// <summary>
/// A request to elevate privileges (Just-In-Time).
/// </summary>
public class PrivilegeElevationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequesterId { get; set; } = string.Empty;
    public Guid PrivilegeId { get; set; }
    public PrivilegeDefinition Privilege { get; set; } = null!;
    public string Justification { get; set; } = string.Empty;
    public int RequestedDurationMinutes { get; set; } = 60;
    public ElevationStatus Status { get; set; } = ElevationStatus.Pending;
    public string? ApproverId { get; set; }
    public string? ApproverNote { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Audit log of privileged actions performed during an elevation.
/// </summary>
public class PrivilegedActionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ElevationRequestId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PrivilegeCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string? Details { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}

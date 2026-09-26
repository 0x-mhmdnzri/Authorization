namespace Authorization.API.Models;

/// <summary>
/// Rule-Based Access Control (RuBAC).
/// Predefined if-then rules evaluated at request time.
/// Independent of (or layered on) roles – focuses on conditions.
/// Classic example: firewall / time / IP rules.
/// </summary>
public class AccessRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Higher = evaluated first. Deny rules often have higher priority.</summary>
    public int Priority { get; set; } = 100;

    public string Effect { get; set; } = "Allow"; // Allow | Deny
    public bool IsEnabled { get; set; } = true;

    // Target
    public string? ResourceType { get; set; } // null or * = any
    public string? Action { get; set; }       // null or * = any

    // ---- Rule conditions (all specified conditions must match) ----

    /// <summary>Comma-separated IP CIDR or exact IPs. Empty = any.</summary>
    public string? SourceIpAllowList { get; set; }

    /// <summary>Comma-separated IPs to always deny when matched.</summary>
    public string? SourceIpDenyList { get; set; }

    /// <summary>UTC hour start (inclusive). Null = no time restriction.</summary>
    public int? TimeStartHour { get; set; }
    public int? TimeEndHour { get; set; }

    /// <summary>Mon,Tue,Wed,Thu,Fri,Sat,Sun – empty = any day.</summary>
    public string? DaysOfWeek { get; set; }

    /// <summary>Required department (exact). Empty = any.</summary>
    public string? RequiredDepartment { get; set; }

    /// <summary>Required role (user must have at least one). Empty = any.</summary>
    public string? RequiredRole { get; set; }

    /// <summary>Max number of requests per user per hour (0 = unlimited).</summary>
    public int RateLimitPerHour { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Simple counter for rate-limit rules.
/// </summary>
public class RuleRateCounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid RuleId { get; set; }
    public int Count { get; set; }
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
}

namespace Authorization.API.Models;

/// <summary>
/// Context-Based Access Control (CBAC) policy.
/// Access depends on environmental / situational context:
/// time, network, device, location, etc.
/// </summary>
public class ContextPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Resource type this applies to, or * for any.</summary>
    public string ResourceType { get; set; } = "*";

    /// <summary>Action: Read, Write, * </summary>
    public string Action { get; set; } = "*";

    public string Effect { get; set; } = "Allow"; // Allow | Deny
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    // ---- Context conditions ----

    /// <summary>Allowed hour range start (UTC, 0-23). Null = no restriction.</summary>
    public int? AllowedHourStart { get; set; }
    public int? AllowedHourEnd { get; set; }

    /// <summary>Comma-separated allowed days: Mon,Tue,Wed,Thu,Fri,Sat,Sun</summary>
    public string? AllowedDaysOfWeek { get; set; }

    /// <summary>Require corporate / trusted network.</summary>
    public bool RequireTrustedNetwork { get; set; }

    /// <summary>Require managed / compliant device.</summary>
    public bool RequireManagedDevice { get; set; }

    /// <summary>Comma-separated allowed country codes (ISO). Empty = any.</summary>
    public string? AllowedCountries { get; set; }

    /// <summary>Comma-separated blocked country codes.</summary>
    public string? BlockedCountries { get; set; }

    /// <summary>Minimum auth method strength: Password, MFA, Certificate</summary>
    public string? MinimumAuthMethod { get; set; }

    /// <summary>Block if session is from anomalous location.</summary>
    public bool BlockAnomalousLocation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Audit log for context evaluations.
/// </summary>
public class ContextEvaluationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string Action { get; set; } = "Read";
    public bool Allowed { get; set; }
    public string? Reason { get; set; }
    public string? MatchedPolicy { get; set; }
    public string? ContextSnapshot { get; set; } // JSON-ish summary
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

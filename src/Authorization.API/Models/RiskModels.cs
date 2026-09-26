namespace Authorization.API.Models;

/// <summary>
/// Risk-Adaptive Access Control (RAdAC) configuration / thresholds.
/// Access is granted by comparing real-time risk score against operational need.
/// </summary>
public class RiskPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Risk score below this → Allow normally.</summary>
    public double NormalRiskThreshold { get; set; } = 30;

    /// <summary>Risk score below this can still be allowed if operational need is high.</summary>
    public double MaxAcceptableRisk { get; set; } = 70;

    /// <summary>Operational need score that can override elevated risk.</summary>
    public double CriticalNeedThreshold { get; set; } = 80;

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Snapshot of risk factors for an access request (can be stored for audit).
/// </summary>
public class RiskAssessmentLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string Action { get; set; } = "Read";

    public double RiskScore { get; set; }
    public double OperationalNeedScore { get; set; }
    public bool Allowed { get; set; }
    public string Decision { get; set; } = string.Empty; // Allow, AllowWithConstraints, Deny
    public string? Reason { get; set; }

    // Factor breakdown (0-100 each contribution or raw)
    public double DeviceRisk { get; set; }
    public double LocationRisk { get; set; }
    public double TimeRisk { get; set; }
    public double AuthStrengthRisk { get; set; }
    public double BehavioralRisk { get; set; }

    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

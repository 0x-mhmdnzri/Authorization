using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

/// <summary>
/// Context supplied by the caller (or enriched by infrastructure) for risk evaluation.
/// </summary>
public class RiskContext
{
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
    public bool IsManagedDevice { get; set; }
    public bool IsTrustedNetwork { get; set; }
    public string? CountryCode { get; set; }
    public string AuthMethod { get; set; } = "Password"; // Password, MFA, Certificate, ...
    public int FailedLoginsLastHour { get; set; }
    public bool IsAnomalousLocation { get; set; }
    public bool IsOffHours { get; set; }
    /// <summary>0-100: how critical is this access for the mission / operation.</summary>
    public double OperationalNeed { get; set; } = 50;
}

public record RadacDecision(
    bool Allowed,
    string Decision,          // Allow | AllowWithConstraints | Deny
    double RiskScore,
    double OperationalNeedScore,
    string Reason,
    Dictionary<string, double>? FactorBreakdown = null
);

public interface IRadacService
{
    Task<RadacDecision> EvaluateAsync(ApplicationUser subject, Guid? resourceId, string action, RiskContext context);
    Task<IEnumerable<RiskPolicy>> GetPoliciesAsync();
}

public class RadacService : IRadacService
{
    private readonly ApplicationDbContext _db;

    public RadacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<RiskPolicy>> GetPoliciesAsync()
    {
        return await _db.RiskPolicies.AsNoTracking()
            .Where(p => p.IsEnabled)
            .ToListAsync();
    }

    public async Task<RadacDecision> EvaluateAsync(
        ApplicationUser subject,
        Guid? resourceId,
        string action,
        RiskContext ctx)
    {
        var policy = await _db.RiskPolicies.AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync()
            ?? new RiskPolicy(); // defaults

        // ---- Compute factor risks (0 = low risk, 100 = high risk) ----
        double deviceRisk = ctx.IsManagedDevice ? 5 : 40;
        if (string.IsNullOrEmpty(ctx.DeviceId)) deviceRisk += 15;

        double locationRisk = ctx.IsTrustedNetwork ? 5 : 25;
        if (ctx.IsAnomalousLocation) locationRisk += 35;
        if (!string.IsNullOrEmpty(ctx.CountryCode) &&
            ctx.CountryCode is not ("IR" or "US" or "DE" or "GB" or "NL"))
            locationRisk += 20;
        locationRisk = Math.Min(100, locationRisk);

        double timeRisk = ctx.IsOffHours ? 30 : 5;
        // also derive from clock if not set
        var hour = DateTime.UtcNow.Hour;
        if (!ctx.IsOffHours && (hour < 6 || hour >= 22)) timeRisk = 25;

        double authRisk = ctx.AuthMethod.ToUpperInvariant() switch
        {
            "CERTIFICATE" or "HARDWAREKEY" => 0,
            "MFA" or "TOTP" or "WEBAUTHN" => 10,
            "PASSWORD" => 35,
            _ => 50
        };

        double behavioralRisk = Math.Min(100, ctx.FailedLoginsLastHour * 20.0);
        // simple clearance-based adjustment: higher clearance slightly lowers perceived risk of the subject
        if (!string.IsNullOrEmpty(subject.ClearanceLevel) &&
            subject.ClearanceLevel is "Secret" or "TopSecret")
            behavioralRisk = Math.Max(0, behavioralRisk - 10);

        // Weighted risk score
        double riskScore =
            deviceRisk * 0.25 +
            locationRisk * 0.25 +
            timeRisk * 0.15 +
            authRisk * 0.20 +
            behavioralRisk * 0.15;

        riskScore = Math.Round(Math.Clamp(riskScore, 0, 100), 2);
        double need = Math.Clamp(ctx.OperationalNeed, 0, 100);

        string decision;
        bool allowed;
        string reason;

        if (riskScore <= policy.NormalRiskThreshold)
        {
            decision = "Allow";
            allowed = true;
            reason = $"Risk ({riskScore}) is within normal threshold ({policy.NormalRiskThreshold}).";
        }
        else if (riskScore <= policy.MaxAcceptableRisk && need >= policy.CriticalNeedThreshold)
        {
            decision = "AllowWithConstraints";
            allowed = true;
            reason = $"Elevated risk ({riskScore}) accepted due to high operational need ({need}). Constraints recommended (time-box, extra audit).";
        }
        else if (riskScore <= policy.MaxAcceptableRisk)
        {
            decision = "Deny";
            allowed = false;
            reason = $"Risk ({riskScore}) is elevated and operational need ({need}) is not critical enough (need threshold {policy.CriticalNeedThreshold}).";
        }
        else
        {
            decision = "Deny";
            allowed = false;
            reason = $"Risk ({riskScore}) exceeds maximum acceptable ({policy.MaxAcceptableRisk}).";
        }

        var breakdown = new Dictionary<string, double>
        {
            ["device"] = deviceRisk,
            ["location"] = locationRisk,
            ["time"] = timeRisk,
            ["auth"] = authRisk,
            ["behavioral"] = behavioralRisk
        };

        // Audit log
        _db.RiskAssessmentLogs.Add(new RiskAssessmentLog
        {
            UserId = subject.Id,
            ResourceId = resourceId,
            Action = action,
            RiskScore = riskScore,
            OperationalNeedScore = need,
            Allowed = allowed,
            Decision = decision,
            Reason = reason,
            DeviceRisk = deviceRisk,
            LocationRisk = locationRisk,
            TimeRisk = timeRisk,
            AuthStrengthRisk = authRisk,
            BehavioralRisk = behavioralRisk
        });
        await _db.SaveChangesAsync();

        return new RadacDecision(allowed, decision, riskScore, need, reason, breakdown);
    }
}

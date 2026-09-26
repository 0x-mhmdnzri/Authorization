using System.Text.Json;
using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

/// <summary>
/// Runtime context supplied with the request (from gateway, client, or enrichment).
/// </summary>
public class AccessContext
{
    public DateTime EvaluationTimeUtc { get; set; } = DateTime.UtcNow;
    public bool IsTrustedNetwork { get; set; }
    public bool IsManagedDevice { get; set; }
    public string? CountryCode { get; set; }
    public bool IsAnomalousLocation { get; set; }
    public string AuthMethod { get; set; } = "Password"; // Password, MFA, Certificate
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
}

public record CbacDecision(
    bool Allowed,
    string Reason,
    string? MatchedPolicyName = null
);

public interface ICbacService
{
    Task<CbacDecision> EvaluateAsync(string userId, string? resourceType, string action, AccessContext context, Guid? resourceId = null);
    Task<IEnumerable<ContextPolicy>> GetPoliciesAsync();
}

public class CbacService : ICbacService
{
    private readonly ApplicationDbContext _db;

    private static readonly Dictionary<string, int> AuthStrength = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Password"] = 1,
        ["MFA"] = 2,
        ["TOTP"] = 2,
        ["WebAuthn"] = 3,
        ["Certificate"] = 3,
        ["HardwareKey"] = 3
    };

    public CbacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ContextPolicy>> GetPoliciesAsync()
    {
        return await _db.ContextPolicies.AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<CbacDecision> EvaluateAsync(
        string userId,
        string? resourceType,
        string action,
        AccessContext ctx,
        Guid? resourceId = null)
    {
        var policies = await _db.ContextPolicies.AsNoTracking()
            .Where(p => p.IsEnabled &&
                        (p.ResourceType == "*" || p.ResourceType == resourceType) &&
                        (p.Action == "*" || p.Action.Equals(action, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        CbacDecision decision;
        if (policies.Count == 0)
        {
            decision = new CbacDecision(false, "No matching CBAC policy. Default deny.");
        }
        else
        {
            decision = new CbacDecision(false, "No CBAC policy conditions matched. Default deny.");
            foreach (var policy in policies)
            {
                var (matches, reason) = Matches(policy, ctx);
                if (matches)
                {
                    var allowed = policy.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                    decision = new CbacDecision(allowed, reason, policy.Name);
                    break;
                }
            }
        }

        // Audit
        _db.ContextEvaluationLogs.Add(new ContextEvaluationLog
        {
            UserId = userId,
            ResourceId = resourceId,
            Action = action,
            Allowed = decision.Allowed,
            Reason = decision.Reason,
            MatchedPolicy = decision.MatchedPolicyName,
            ContextSnapshot = JsonSerializer.Serialize(new
            {
                ctx.EvaluationTimeUtc,
                ctx.IsTrustedNetwork,
                ctx.IsManagedDevice,
                ctx.CountryCode,
                ctx.IsAnomalousLocation,
                ctx.AuthMethod,
                ctx.IpAddress
            })
        });
        await _db.SaveChangesAsync();

        return decision;
    }

    private static (bool Matches, string Reason) Matches(ContextPolicy policy, AccessContext ctx)
    {
        var details = new List<string>();
        var t = ctx.EvaluationTimeUtc;

        // Time window
        if (policy.AllowedHourStart.HasValue && policy.AllowedHourEnd.HasValue)
        {
            var start = policy.AllowedHourStart.Value;
            var end = policy.AllowedHourEnd.Value;
            bool inWindow;
            if (start <= end)
                inWindow = t.Hour >= start && t.Hour < end;
            else
                inWindow = t.Hour >= start || t.Hour < end; // overnight

            if (!inWindow)
                return (false, $"Policy '{policy.Name}': outside allowed hours ({start}:00-{end}:00 UTC).");
            details.Add("time-window");
        }

        // Days of week
        if (!string.IsNullOrWhiteSpace(policy.AllowedDaysOfWeek))
        {
            var day = t.DayOfWeek.ToString()[..3]; // Mon, Tue, ...
            var allowed = policy.AllowedDaysOfWeek
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(d => d.StartsWith(day, StringComparison.OrdinalIgnoreCase) ||
                                  d.Equals(t.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase)))
                return (false, $"Policy '{policy.Name}': day '{t.DayOfWeek}' not allowed.");
            details.Add("day-of-week");
        }

        // Trusted network
        if (policy.RequireTrustedNetwork && !ctx.IsTrustedNetwork)
            return (false, $"Policy '{policy.Name}': trusted network required.");

        if (policy.RequireTrustedNetwork)
            details.Add("trusted-network");

        // Managed device
        if (policy.RequireManagedDevice && !ctx.IsManagedDevice)
            return (false, $"Policy '{policy.Name}': managed device required.");

        if (policy.RequireManagedDevice)
            details.Add("managed-device");

        // Allowed countries
        if (!string.IsNullOrWhiteSpace(policy.AllowedCountries))
        {
            var allowed = policy.AllowedCountries
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (string.IsNullOrEmpty(ctx.CountryCode) ||
                !allowed.Any(c => c.Equals(ctx.CountryCode, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Policy '{policy.Name}': country '{ctx.CountryCode}' not in allow-list.");
            details.Add("country-allow");
        }

        // Blocked countries
        if (!string.IsNullOrWhiteSpace(policy.BlockedCountries) && !string.IsNullOrEmpty(ctx.CountryCode))
        {
            var blocked = policy.BlockedCountries
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (blocked.Any(c => c.Equals(ctx.CountryCode, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Policy '{policy.Name}': country '{ctx.CountryCode}' is blocked.");
        }

        // Auth method strength
        if (!string.IsNullOrWhiteSpace(policy.MinimumAuthMethod))
        {
            var required = AuthStrength.GetValueOrDefault(policy.MinimumAuthMethod, 1);
            var actual = AuthStrength.GetValueOrDefault(ctx.AuthMethod, 0);
            if (actual < required)
                return (false, $"Policy '{policy.Name}': auth method '{ctx.AuthMethod}' below required '{policy.MinimumAuthMethod}'.");
            details.Add("auth-strength");
        }

        // Anomalous location
        if (policy.BlockAnomalousLocation && ctx.IsAnomalousLocation)
            return (false, $"Policy '{policy.Name}': anomalous location blocked.");

        var reason = details.Count > 0
            ? $"CBAC policy '{policy.Name}' matched ({string.Join(", ", details)})."
            : $"CBAC policy '{policy.Name}' matched.";

        return (true, reason);
    }
}

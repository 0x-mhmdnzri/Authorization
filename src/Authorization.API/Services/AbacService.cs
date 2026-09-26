using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public record AbacEvaluationContext(
    ApplicationUser Subject,
    Resource Resource,
    string Action,
    DateTime EvaluationTimeUtc
);

public record AbacDecision(
    bool Allowed,
    string Reason,
    string? MatchedPolicyName = null
);

public interface IAbacService
{
    Task<AbacDecision> EvaluateAsync(AbacEvaluationContext context);
    Task<Resource?> GetResourceAsync(Guid resourceId);
    Task<IEnumerable<AbacPolicy>> GetPoliciesAsync();
}

public class AbacService : IAbacService
{
    private readonly ApplicationDbContext _db;
    private static readonly Dictionary<string, int> ClearanceRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Public"] = 0,
        ["Internal"] = 1,
        ["Confidential"] = 2,
        ["Restricted"] = 3,
        ["Secret"] = 4,
        ["TopSecret"] = 5
    };

    public AbacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Resource?> GetResourceAsync(Guid resourceId)
    {
        return await _db.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);
    }

    public async Task<IEnumerable<AbacPolicy>> GetPoliciesAsync()
    {
        return await _db.AbacPolicies
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<AbacDecision> EvaluateAsync(AbacEvaluationContext context)
    {
        var policies = await _db.AbacPolicies
            .AsNoTracking()
            .Where(p => p.IsEnabled &&
                        (p.ResourceType == null || p.ResourceType == context.Resource.ResourceType) &&
                        p.Action == context.Action)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        if (policies.Count == 0)
        {
            return new AbacDecision(false, "No matching ABAC policy found. Default deny.");
        }

        foreach (var policy in policies)
        {
            var matchResult = EvaluatePolicy(policy, context);
            if (matchResult.IsMatch)
            {
                var allowed = policy.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                return new AbacDecision(
                    allowed,
                    matchResult.Reason,
                    policy.Name
                );
            }
        }

        return new AbacDecision(false, "No policy conditions matched. Default deny.");
    }

    private (bool IsMatch, string Reason) EvaluatePolicy(AbacPolicy policy, AbacEvaluationContext ctx)
    {
        var reasons = new List<string>();

        // 1. Same department check
        if (policy.RequireSameDepartment)
        {
            if (string.IsNullOrEmpty(ctx.Subject.Department) ||
                string.IsNullOrEmpty(ctx.Resource.OwnerDepartment) ||
                !ctx.Subject.Department.Equals(ctx.Resource.OwnerDepartment, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Policy '{policy.Name}': subject department does not match resource owner department.");
            }
            reasons.Add("same department");
        }

        // 2. Allowed departments whitelist
        if (!string.IsNullOrWhiteSpace(policy.AllowedDepartments))
        {
            var allowed = policy.AllowedDepartments
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (string.IsNullOrEmpty(ctx.Subject.Department) ||
                !allowed.Any(d => d.Equals(ctx.Subject.Department, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"Policy '{policy.Name}': subject department not in allowed list.");
            }
            reasons.Add("department whitelist");
        }

        // 3. Business hours (08:00 - 18:00 UTC)
        if (policy.RequireBusinessHours)
        {
            var hour = ctx.EvaluationTimeUtc.Hour;
            if (hour < 8 || hour >= 18)
            {
                return (false, $"Policy '{policy.Name}': outside business hours (08:00-18:00 UTC).");
            }
            reasons.Add("business hours");
        }

        // 4. Minimum clearance level of subject
        if (!string.IsNullOrWhiteSpace(policy.MinimumClearance))
        {
            var subjectRank = GetClearanceRank(ctx.Subject.ClearanceLevel);
            var requiredRank = GetClearanceRank(policy.MinimumClearance);
            if (subjectRank < requiredRank)
            {
                return (false, $"Policy '{policy.Name}': subject clearance '{ctx.Subject.ClearanceLevel}' is below required '{policy.MinimumClearance}'.");
            }
            reasons.Add($"clearance >= {policy.MinimumClearance}");
        }

        // 5. Resource sensitivity must not exceed subject's max allowed
        if (!string.IsNullOrWhiteSpace(policy.RequiredSensitivityMax))
        {
            var resourceRank = GetClearanceRank(ctx.Resource.Sensitivity);
            var maxAllowedRank = GetClearanceRank(policy.RequiredSensitivityMax);
            if (resourceRank > maxAllowedRank)
            {
                return (false, $"Policy '{policy.Name}': resource sensitivity '{ctx.Resource.Sensitivity}' exceeds max allowed '{policy.RequiredSensitivityMax}'.");
            }
            reasons.Add($"sensitivity <= {policy.RequiredSensitivityMax}");
        }

        var reason = reasons.Count > 0
            ? $"All conditions of policy '{policy.Name}' matched ({string.Join(", ", reasons)})."
            : $"Policy '{policy.Name}' matched (no extra conditions).";

        return (true, reason);
    }

    private static int GetClearanceRank(string? level)
    {
        if (string.IsNullOrWhiteSpace(level)) return 0;
        return ClearanceRank.TryGetValue(level, out var rank) ? rank : 0;
    }
}

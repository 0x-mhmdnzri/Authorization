using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public record PbacDecision(
    bool Allowed,
    string Reason,
    string? MatchedPolicyName = null,
    int? MatchedPriority = null
);

public interface IPbacService
{
    Task<PbacDecision> EvaluateAsync(ApplicationUser subject, IList<string> roles, Resource resource, string action);
    Task<IEnumerable<Policy>> GetPoliciesAsync();
    Task<Policy?> GetPolicyAsync(Guid id);
}

/// <summary>
/// Policy-Based Access Control – central Policy Decision Point (PDP).
/// Evaluates ordered policies that can combine roles, attributes, context and DAC grants.
/// </summary>
public class PbacService : IPbacService
{
    private readonly ApplicationDbContext _db;
    private readonly IDacService _dacService;

    private static readonly Dictionary<string, int> ClearanceRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Public"] = 0, ["Internal"] = 1, ["Confidential"] = 2,
        ["Restricted"] = 3, ["Secret"] = 4, ["TopSecret"] = 5
    };

    public PbacService(ApplicationDbContext db, IDacService dacService)
    {
        _db = db;
        _dacService = dacService;
    }

    public async Task<IEnumerable<Policy>> GetPoliciesAsync()
    {
        return await _db.Policies.AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<Policy?> GetPolicyAsync(Guid id)
    {
        return await _db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PbacDecision> EvaluateAsync(
        ApplicationUser subject,
        IList<string> roles,
        Resource resource,
        string action)
    {
        var policies = await _db.Policies.AsNoTracking()
            .Where(p => p.IsEnabled &&
                        (p.ResourceType == "*" || p.ResourceType == resource.ResourceType) &&
                        (p.Action == "*" || p.Action.Equals(action, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        if (policies.Count == 0)
            return new PbacDecision(false, "No matching PBAC policy. Default deny.");

        var now = DateTime.UtcNow;

        foreach (var policy in policies)
        {
            var (matches, reason) = await MatchesAsync(policy, subject, roles, resource, action, now);
            if (matches)
            {
                var allowed = policy.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                return new PbacDecision(
                    allowed,
                    reason,
                    policy.Name,
                    policy.Priority
                );
            }
        }

        return new PbacDecision(false, "No PBAC policy conditions matched. Default deny.");
    }

    private async Task<(bool Matches, string Reason)> MatchesAsync(
        Policy policy,
        ApplicationUser subject,
        IList<string> roles,
        Resource resource,
        string action,
        DateTime now)
    {
        var details = new List<string>();

        // Roles
        if (!string.IsNullOrWhiteSpace(policy.RequiredRoles))
        {
            var required = policy.RequiredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!required.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                return (false, $"Policy '{policy.Name}': required role not present.");
            details.Add("role");
        }

        // Departments
        if (!string.IsNullOrWhiteSpace(policy.RequiredDepartments))
        {
            var allowed = policy.RequiredDepartments.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (string.IsNullOrEmpty(subject.Department) ||
                !allowed.Any(d => d.Equals(subject.Department, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Policy '{policy.Name}': department not allowed.");
            details.Add("department");
        }

        // Same department
        if (policy.RequireSameDepartment)
        {
            if (string.IsNullOrEmpty(subject.Department) ||
                string.IsNullOrEmpty(resource.OwnerDepartment) ||
                !subject.Department.Equals(resource.OwnerDepartment, StringComparison.OrdinalIgnoreCase))
                return (false, $"Policy '{policy.Name}': department mismatch.");
            details.Add("same-department");
        }

        // Clearance
        if (!string.IsNullOrWhiteSpace(policy.MinimumClearance))
        {
            var subjRank = GetRank(subject.ClearanceLevel);
            var reqRank = GetRank(policy.MinimumClearance);
            if (subjRank < reqRank)
                return (false, $"Policy '{policy.Name}': clearance too low.");
            details.Add("clearance");
        }

        // Max resource sensitivity
        if (!string.IsNullOrWhiteSpace(policy.MaxResourceSensitivity))
        {
            var resRank = GetRank(resource.Sensitivity);
            var maxRank = GetRank(policy.MaxResourceSensitivity);
            if (resRank > maxRank)
                return (false, $"Policy '{policy.Name}': resource sensitivity too high.");
            details.Add("sensitivity");
        }

        // Business hours
        if (policy.RequireBusinessHours)
        {
            if (now.Hour < 8 || now.Hour >= 18)
                return (false, $"Policy '{policy.Name}': outside business hours.");
            details.Add("business-hours");
        }

        // Optional DAC grant requirement
        if (policy.RequireDacGrant)
        {
            var dac = await _dacService.CanAccessAsync(subject.Id, resource.Id, action);
            if (!dac.Allowed && !dac.IsOwner)
                return (false, $"Policy '{policy.Name}': missing DAC grant.");
            details.Add("dac-grant");
        }

        var reason = details.Count > 0
            ? $"PBAC policy '{policy.Name}' matched ({string.Join(", ", details)})."
            : $"PBAC policy '{policy.Name}' matched (no extra conditions).";

        return (true, reason);
    }

    private static int GetRank(string? level)
    {
        if (string.IsNullOrWhiteSpace(level)) return 0;
        return ClearanceRank.TryGetValue(level, out var r) ? r : 0;
    }
}

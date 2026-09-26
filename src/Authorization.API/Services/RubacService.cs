using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public class RuleEvaluationContext
{
    public string UserId { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public string? Department { get; set; }
    public string? ResourceType { get; set; }
    public string Action { get; set; } = "Read";
    public string? SourceIp { get; set; }
    public DateTime EvaluationTimeUtc { get; set; } = DateTime.UtcNow;
}

public record RubacDecision(
    bool Allowed,
    string Reason,
    string? MatchedRuleName = null,
    int? MatchedPriority = null
);

public interface IRubacService
{
    Task<RubacDecision> EvaluateAsync(RuleEvaluationContext context);
    Task<IEnumerable<AccessRule>> GetRulesAsync();
}

public class RubacService : IRubacService
{
    private readonly ApplicationDbContext _db;

    public RubacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<AccessRule>> GetRulesAsync()
    {
        return await _db.AccessRules.AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
    }

    public async Task<RubacDecision> EvaluateAsync(RuleEvaluationContext ctx)
    {
        var rules = await _db.AccessRules.AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        if (rules.Count == 0)
            return new RubacDecision(false, "No RuBAC rules defined. Default deny.");

        foreach (var rule in rules)
        {
            // Target filter
            if (!string.IsNullOrEmpty(rule.ResourceType) && rule.ResourceType != "*" &&
                !string.Equals(rule.ResourceType, ctx.ResourceType, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(rule.Action) && rule.Action != "*" &&
                !string.Equals(rule.Action, ctx.Action, StringComparison.OrdinalIgnoreCase))
                continue;

            var (matches, failReason) = await MatchesRuleAsync(rule, ctx);
            if (!matches)
                continue;

            var allowed = rule.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase);
            return new RubacDecision(
                allowed,
                allowed
                    ? $"RuBAC rule '{rule.Name}' matched → Allow."
                    : $"RuBAC rule '{rule.Name}' matched → Deny. {failReason}".Trim(),
                rule.Name,
                rule.Priority
            );
        }

        return new RubacDecision(false, "No RuBAC rule matched. Default deny.");
    }

    private async Task<(bool Matches, string Reason)> MatchesRuleAsync(AccessRule rule, RuleEvaluationContext ctx)
    {
        // IP deny list (if IP is on deny list, this deny-rule can match)
        if (!string.IsNullOrWhiteSpace(rule.SourceIpDenyList) && !string.IsNullOrEmpty(ctx.SourceIp))
        {
            var denied = rule.SourceIpDenyList
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (denied.Any(ip => IpMatches(ctx.SourceIp, ip)))
            {
                // For a Deny rule this is a match; for Allow rule, IP on deny list means no match
                if (rule.Effect.Equals("Deny", StringComparison.OrdinalIgnoreCase))
                    return (true, $"IP {ctx.SourceIp} on deny list.");
                return (false, "IP on deny list.");
            }
        }

        // IP allow list
        if (!string.IsNullOrWhiteSpace(rule.SourceIpAllowList))
        {
            if (string.IsNullOrEmpty(ctx.SourceIp))
                return (false, "Source IP required but missing.");
            var allowed = rule.SourceIpAllowList
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(ip => IpMatches(ctx.SourceIp, ip)))
                return (false, "IP not in allow list.");
        }

        // Time window
        if (rule.TimeStartHour.HasValue && rule.TimeEndHour.HasValue)
        {
            var h = ctx.EvaluationTimeUtc.Hour;
            var start = rule.TimeStartHour.Value;
            var end = rule.TimeEndHour.Value;
            bool inWindow = start <= end ? (h >= start && h < end) : (h >= start || h < end);
            if (!inWindow)
                return (false, "Outside time window.");
        }

        // Days of week
        if (!string.IsNullOrWhiteSpace(rule.DaysOfWeek))
        {
            var day = ctx.EvaluationTimeUtc.DayOfWeek.ToString();
            var days = rule.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!days.Any(d => day.StartsWith(d, StringComparison.OrdinalIgnoreCase) ||
                               d.Equals(day, StringComparison.OrdinalIgnoreCase)))
                return (false, "Day not allowed.");
        }

        // Department
        if (!string.IsNullOrWhiteSpace(rule.RequiredDepartment))
        {
            if (string.IsNullOrEmpty(ctx.Department) ||
                !ctx.Department.Equals(rule.RequiredDepartment, StringComparison.OrdinalIgnoreCase))
                return (false, "Department mismatch.");
        }

        // Role
        if (!string.IsNullOrWhiteSpace(rule.RequiredRole))
        {
            if (!ctx.Roles.Contains(rule.RequiredRole, StringComparer.OrdinalIgnoreCase))
                return (false, "Required role missing.");
        }

        // Rate limit
        if (rule.RateLimitPerHour > 0)
        {
            var windowStart = ctx.EvaluationTimeUtc.AddHours(-1);
            var counter = await _db.RuleRateCounters
                .FirstOrDefaultAsync(c => c.UserId == ctx.UserId && c.RuleId == rule.Id);

            if (counter == null)
            {
                _db.RuleRateCounters.Add(new RuleRateCounter
                {
                    UserId = ctx.UserId,
                    RuleId = rule.Id,
                    Count = 1,
                    WindowStart = ctx.EvaluationTimeUtc
                });
                await _db.SaveChangesAsync();
            }
            else if (counter.WindowStart < windowStart)
            {
                counter.Count = 1;
                counter.WindowStart = ctx.EvaluationTimeUtc;
                await _db.SaveChangesAsync();
            }
            else
            {
                counter.Count++;
                await _db.SaveChangesAsync();
                if (counter.Count > rule.RateLimitPerHour)
                    return (false, $"Rate limit exceeded ({rule.RateLimitPerHour}/hour).");
            }
        }

        return (true, string.Empty);
    }

    private static bool IpMatches(string sourceIp, string pattern)
    {
        // Exact match or simple prefix (demo – not full CIDR parser)
        if (pattern.Contains('/'))
        {
            var prefix = pattern.Split('/')[0];
            // naive: match first 3 octets for /24 style demos
            var srcParts = sourceIp.Split('.');
            var preParts = prefix.Split('.');
            if (srcParts.Length >= 3 && preParts.Length >= 3)
                return srcParts[0] == preParts[0] && srcParts[1] == preParts[1] && srcParts[2] == preParts[2];
        }
        return sourceIp.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
               sourceIp.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
    }
}

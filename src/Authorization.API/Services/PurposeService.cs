using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public record PurposeDecision(
    bool Allowed,
    string Reason,
    string? PurposeCode = null
);

public interface IPurposeService
{
    Task<PurposeDecision> EvaluateAsync(string userId, IList<string> roles, Guid resourceId, string purposeCode, string action = "Read");
    Task<IEnumerable<Purpose>> GetPurposesAsync();
    Task<IEnumerable<ResourcePurpose>> GetResourcePurposesAsync(Guid resourceId);
    Task LogAccessAsync(string userId, Guid resourceId, Guid purposeId, string action, bool allowed, string? reason);
}

public class PurposeService : IPurposeService
{
    private readonly ApplicationDbContext _db;

    public PurposeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Purpose>> GetPurposesAsync()
    {
        return await _db.Purposes.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<IEnumerable<ResourcePurpose>> GetResourcePurposesAsync(Guid resourceId)
    {
        return await _db.ResourcePurposes.AsNoTracking()
            .Include(rp => rp.Purpose)
            .Where(rp => rp.ResourceId == resourceId)
            .ToListAsync();
    }

    public async Task<PurposeDecision> EvaluateAsync(
        string userId,
        IList<string> roles,
        Guid resourceId,
        string purposeCode,
        string action = "Read")
    {
        var purpose = await _db.Purposes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == purposeCode && p.IsActive);

        if (purpose == null)
            return new PurposeDecision(false, $"Unknown or inactive purpose code '{purposeCode}'.");

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);

        if (resource == null)
            return new PurposeDecision(false, "Resource not found or inactive.", purpose.Code);

        var link = await _db.ResourcePurposes.AsNoTracking()
            .Include(rp => rp.Purpose)
            .FirstOrDefaultAsync(rp => rp.ResourceId == resourceId && rp.PurposeId == purpose.Id);

        if (link == null)
        {
            await LogAccessAsync(userId, resourceId, purpose.Id, action, false,
                $"Purpose '{purposeCode}' is not allowed on this resource.");
            return new PurposeDecision(false,
                $"Purpose '{purposeCode}' is not permitted for resource '{resource.Name}'.",
                purpose.Code);
        }

        // Role restriction on this purpose for the resource
        if (!string.IsNullOrWhiteSpace(link.AllowedRoles))
        {
            var allowed = link.AllowedRoles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                var reason = $"Purpose '{purposeCode}' on this resource requires one of roles: [{link.AllowedRoles}].";
                await LogAccessAsync(userId, resourceId, purpose.Id, action, false, reason);
                return new PurposeDecision(false, reason, purpose.Code);
            }
        }

        // Consent flag (demo: we just record it; real systems would check consent store)
        if (link.RequiresExplicitConsent)
        {
            // In a full implementation we would check a Consent table.
            // For demo we allow but note it in the reason.
        }

        var okReason = $"Access allowed for purpose '{purpose.Name}' ({purpose.Code}).";
        await LogAccessAsync(userId, resourceId, purpose.Id, action, true, okReason);
        return new PurposeDecision(true, okReason, purpose.Code);
    }

    public async Task LogAccessAsync(string userId, Guid resourceId, Guid purposeId, string action, bool allowed, string? reason)
    {
        _db.PurposeAccessLogs.Add(new PurposeAccessLog
        {
            UserId = userId,
            ResourceId = resourceId,
            PurposeId = purposeId,
            Action = action,
            Allowed = allowed,
            Reason = reason
        });
        await _db.SaveChangesAsync();
    }
}

using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public record DacDecision(
    bool Allowed,
    string Reason,
    bool IsOwner = false
);

public interface IDacService
{
    Task<DacDecision> CanAccessAsync(string userId, Guid resourceId, string permission);
    Task<ResourcePermission?> GrantAsync(string ownerId, Guid resourceId, string subjectId, string permissions, DateTime? expiresAt = null);
    Task<bool> RevokeAsync(string ownerId, Guid resourceId, string subjectId);
    Task<IEnumerable<ResourcePermission>> GetAclAsync(Guid resourceId);
    Task<Resource?> GetResourceAsync(Guid resourceId);
}

public class DacService : IDacService
{
    private readonly ApplicationDbContext _db;

    public DacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Resource?> GetResourceAsync(Guid resourceId)
    {
        return await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);
    }

    public async Task<DacDecision> CanAccessAsync(string userId, Guid resourceId, string permission)
    {
        var resource = await GetResourceAsync(resourceId);
        if (resource == null)
            return new DacDecision(false, "Resource not found or inactive.");

        // Owner always has full access (classic DAC)
        if (resource.OwnerId == userId)
            return new DacDecision(true, "Access granted: user is the resource owner.", IsOwner: true);

        var entry = await _db.ResourcePermissions.AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.ResourceId == resourceId &&
                p.SubjectId == userId &&
                p.IsActive &&
                (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));

        if (entry == null)
            return new DacDecision(false, "No ACL entry found for this user on the resource.");

        var perms = entry.Permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLowerInvariant())
            .ToHashSet();

        var needed = permission.Trim().ToLowerInvariant();
        if (perms.Contains(needed) || perms.Contains("full") || perms.Contains("*"))
            return new DacDecision(true, $"Access granted via ACL: [{entry.Permissions}].");

        return new DacDecision(false, $"ACL entry exists but does not include permission '{permission}'. Granted: [{entry.Permissions}].");
    }

    public async Task<ResourcePermission?> GrantAsync(
        string granterId, Guid resourceId, string subjectId, string permissions, DateTime? expiresAt = null)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);
        if (resource == null) return null;

        // Only owner (or someone who already has Share) can grant
        var canShare = resource.OwnerId == granterId;
        if (!canShare)
        {
            var shareEntry = await _db.ResourcePermissions.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ResourceId == resourceId &&
                    p.SubjectId == granterId &&
                    p.IsActive &&
                    (p.Permissions.Contains("Share", StringComparison.OrdinalIgnoreCase) ||
                     p.Permissions.Contains("Full", StringComparison.OrdinalIgnoreCase) ||
                     p.Permissions.Contains("*")));
            canShare = shareEntry != null;
        }

        if (!canShare) return null;

        var existing = await _db.ResourcePermissions
            .FirstOrDefaultAsync(p => p.ResourceId == resourceId && p.SubjectId == subjectId);

        if (existing != null)
        {
            existing.Permissions = permissions;
            existing.GrantedById = granterId;
            existing.GrantedAt = DateTime.UtcNow;
            existing.ExpiresAt = expiresAt;
            existing.IsActive = true;
        }
        else
        {
            existing = new ResourcePermission
            {
                ResourceId = resourceId,
                SubjectId = subjectId,
                Permissions = permissions,
                GrantedById = granterId,
                ExpiresAt = expiresAt
            };
            _db.ResourcePermissions.Add(existing);
        }

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> RevokeAsync(string ownerId, Guid resourceId, string subjectId)
    {
        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null || resource.OwnerId != ownerId)
            return false; // only owner can revoke in this simple model

        var entry = await _db.ResourcePermissions
            .FirstOrDefaultAsync(p => p.ResourceId == resourceId && p.SubjectId == subjectId);

        if (entry == null) return false;

        entry.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ResourcePermission>> GetAclAsync(Guid resourceId)
    {
        return await _db.ResourcePermissions.AsNoTracking()
            .Where(p => p.ResourceId == resourceId && p.IsActive)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync();
    }
}

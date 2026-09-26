using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public interface IPacService
{
    Task<PrivilegeElevationRequest?> RequestElevationAsync(
        string requesterId, IList<string> requesterRoles,
        Guid privilegeId, string justification, int durationMinutes);

    Task<PrivilegeElevationRequest?> DecideAsync(
        Guid requestId, string approverId, IList<string> approverRoles,
        bool approve, string? note);

    Task<bool> HasActivePrivilegeAsync(string userId, string privilegeCode);
    Task<IEnumerable<PrivilegeElevationRequest>> GetActiveElevationsAsync(string userId);
    Task RevokeAsync(Guid requestId, string revokedBy);
    Task LogActionAsync(string userId, Guid elevationId, string privilegeCode, string action, string? resource, string? details);
}

public class PacService : IPacService
{
    private readonly ApplicationDbContext _db;

    public PacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PrivilegeElevationRequest?> RequestElevationAsync(
        string requesterId, IList<string> requesterRoles,
        Guid privilegeId, string justification, int durationMinutes)
    {
        var priv = await _db.PrivilegeDefinitions
            .FirstOrDefaultAsync(p => p.Id == privilegeId && p.IsActive);
        if (priv == null) return null;

        // Check requester role eligibility
        if (!string.IsNullOrWhiteSpace(priv.AllowedRequesterRoles))
        {
            var allowed = priv.AllowedRequesterRoles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(r => requesterRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                return null;
        }

        var duration = Math.Clamp(durationMinutes, 5, priv.MaxDurationMinutes);

        var request = new PrivilegeElevationRequest
        {
            RequesterId = requesterId,
            PrivilegeId = privilegeId,
            Justification = justification,
            RequestedDurationMinutes = duration,
            Status = priv.RequiresApproval ? ElevationStatus.Pending : ElevationStatus.Approved
        };

        if (!priv.RequiresApproval)
        {
            request.Status = ElevationStatus.Active;
            request.DecidedAt = DateTime.UtcNow;
            request.StartsAt = DateTime.UtcNow;
            request.ExpiresAt = DateTime.UtcNow.AddMinutes(duration);
        }

        _db.PrivilegeElevationRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<PrivilegeElevationRequest?> DecideAsync(
        Guid requestId, string approverId, IList<string> approverRoles,
        bool approve, string? note)
    {
        var request = await _db.PrivilegeElevationRequests
            .Include(r => r.Privilege)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Status != ElevationStatus.Pending)
            return null;

        // Approver role check
        if (!string.IsNullOrWhiteSpace(request.Privilege.ApproverRoles))
        {
            var allowed = request.Privilege.ApproverRoles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(r => approverRoles.Contains(r, StringComparer.OrdinalIgnoreCase)) &&
                !approverRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                return null;
        }
        else if (!approverRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        request.ApproverId = approverId;
        request.ApproverNote = note;
        request.DecidedAt = DateTime.UtcNow;

        if (approve)
        {
            request.Status = ElevationStatus.Active;
            request.StartsAt = DateTime.UtcNow;
            request.ExpiresAt = DateTime.UtcNow.AddMinutes(request.RequestedDurationMinutes);
        }
        else
        {
            request.Status = ElevationStatus.Denied;
        }

        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<bool> HasActivePrivilegeAsync(string userId, string privilegeCode)
    {
        var now = DateTime.UtcNow;
        return await _db.PrivilegeElevationRequests
            .Include(r => r.Privilege)
            .AnyAsync(r =>
                r.RequesterId == userId &&
                r.Privilege.Code == privilegeCode &&
                r.Status == ElevationStatus.Active &&
                r.ExpiresAt != null && r.ExpiresAt > now);
    }

    public async Task<IEnumerable<PrivilegeElevationRequest>> GetActiveElevationsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        // Expire old ones
        var expired = await _db.PrivilegeElevationRequests
            .Where(r => r.RequesterId == userId && r.Status == ElevationStatus.Active &&
                        r.ExpiresAt != null && r.ExpiresAt <= now)
            .ToListAsync();
        foreach (var e in expired)
            e.Status = ElevationStatus.Expired;
        if (expired.Count > 0)
            await _db.SaveChangesAsync();

        return await _db.PrivilegeElevationRequests.AsNoTracking()
            .Include(r => r.Privilege)
            .Where(r => r.RequesterId == userId && r.Status == ElevationStatus.Active &&
                        r.ExpiresAt != null && r.ExpiresAt > now)
            .ToListAsync();
    }

    public async Task RevokeAsync(Guid requestId, string revokedBy)
    {
        var request = await _db.PrivilegeElevationRequests.FindAsync(requestId);
        if (request == null) return;
        if (request.Status == ElevationStatus.Active || request.Status == ElevationStatus.Pending)
        {
            request.Status = ElevationStatus.Revoked;
            request.ApproverNote = (request.ApproverNote ?? "") + $" [Revoked by {revokedBy}]";
            await _db.SaveChangesAsync();
        }
    }

    public async Task LogActionAsync(string userId, Guid elevationId, string privilegeCode, string action, string? resource, string? details)
    {
        _db.PrivilegedActionLogs.Add(new PrivilegedActionLog
        {
            ElevationRequestId = elevationId,
            UserId = userId,
            PrivilegeCode = privilegeCode,
            Action = action,
            Resource = resource,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}

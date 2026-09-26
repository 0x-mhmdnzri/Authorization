using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

public record RebacDecision(
    bool Allowed,
    string Reason,
    string? MatchedRelation = null
);

public interface IRebacService
{
    Task WriteTupleAsync(string objectType, string objectId, string relation, string subject, string? createdBy = null);
    Task<bool> DeleteTupleAsync(string objectType, string objectId, string relation, string subject);
    Task<IEnumerable<RelationTuple>> ListTuplesAsync(string objectType, string objectId);
    Task<RebacDecision> CheckAsync(string userId, string objectType, string objectId, string permission);
}

/// <summary>
/// Simplified Zanzibar-style ReBAC engine.
/// Supports direct relations and one-level userset (group#member).
/// Permission expansion (example schema):
///   viewer = owner + editor + viewer
///   editor = owner + editor
/// </summary>
public class RebacService : IRebacService
{
    private readonly ApplicationDbContext _db;

    // Simple permission → relations mapping (can be made configurable later)
    private static readonly Dictionary<string, string[]> PermissionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["owner"] = new[] { "owner" },
        ["edit"] = new[] { "owner", "editor" },
        ["write"] = new[] { "owner", "editor" },
        ["view"] = new[] { "owner", "editor", "viewer" },
        ["read"] = new[] { "owner", "editor", "viewer" },
        ["share"] = new[] { "owner" }
    };

    public RebacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task WriteTupleAsync(string objectType, string objectId, string relation, string subject, string? createdBy = null)
    {
        var exists = await _db.RelationTuples.AnyAsync(t =>
            t.ObjectType == objectType &&
            t.ObjectId == objectId &&
            t.Relation == relation &&
            t.Subject == subject);

        if (exists) return;

        _db.RelationTuples.Add(new RelationTuple
        {
            ObjectType = objectType,
            ObjectId = objectId,
            Relation = relation,
            Subject = subject,
            CreatedBy = createdBy
        });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteTupleAsync(string objectType, string objectId, string relation, string subject)
    {
        var tuple = await _db.RelationTuples.FirstOrDefaultAsync(t =>
            t.ObjectType == objectType &&
            t.ObjectId == objectId &&
            t.Relation == relation &&
            t.Subject == subject);

        if (tuple == null) return false;
        _db.RelationTuples.Remove(tuple);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<RelationTuple>> ListTuplesAsync(string objectType, string objectId)
    {
        return await _db.RelationTuples.AsNoTracking()
            .Where(t => t.ObjectType == objectType && t.ObjectId == objectId)
            .OrderBy(t => t.Relation)
            .ToListAsync();
    }

    public async Task<RebacDecision> CheckAsync(string userId, string objectType, string objectId, string permission)
    {
        var relationsToCheck = PermissionMap.TryGetValue(permission, out var rels)
            ? rels
            : new[] { permission };

        var userSubject = $"user:{userId}";

        // 1) Direct user relations
        var direct = await _db.RelationTuples.AsNoTracking()
            .Where(t =>
                t.ObjectType == objectType &&
                t.ObjectId == objectId &&
                relationsToCheck.Contains(t.Relation) &&
                t.Subject == userSubject)
            .Select(t => t.Relation)
            .FirstOrDefaultAsync();

        if (direct != null)
            return new RebacDecision(true, $"Direct relation '{direct}' grants '{permission}'.", direct);

        // 2) Group membership: subject = group:xxx#member
        var groupTuples = await _db.RelationTuples.AsNoTracking()
            .Where(t =>
                t.ObjectType == objectType &&
                t.ObjectId == objectId &&
                relationsToCheck.Contains(t.Relation) &&
                t.Subject.StartsWith("group:") &&
                t.Subject.EndsWith("#member"))
            .ToListAsync();

        foreach (var gt in groupTuples)
        {
            // gt.Subject e.g. group:eng#member → look for group:eng#member@user:alice
            // We store membership as: object=group:eng, relation=member, subject=user:alice
            var parts = gt.Subject.Split('#');
            if (parts.Length != 2) continue;
            var groupObject = parts[0]; // group:eng
            var groupParts = groupObject.Split(':', 2);
            if (groupParts.Length != 2) continue;

            var isMember = await _db.RelationTuples.AsNoTracking().AnyAsync(t =>
                t.ObjectType == groupParts[0] &&
                t.ObjectId == groupParts[1] &&
                t.Relation == "member" &&
                t.Subject == userSubject);

            if (isMember)
                return new RebacDecision(true,
                    $"Group relation '{gt.Relation}' via '{gt.Subject}' grants '{permission}'.",
                    gt.Relation);
        }

        // 3) Simple parent inheritance: if object has parent, check parent for viewer-like perms
        if (permission is "view" or "read")
        {
            var parentTuple = await _db.RelationTuples.AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.ObjectType == objectType &&
                    t.ObjectId == objectId &&
                    t.Relation == "parent");

            if (parentTuple != null)
            {
                // parent subject is folder:xxx or document:yyy
                var parentSubject = parentTuple.Subject;
                var pParts = parentSubject.Split(':', 2);
                if (pParts.Length == 2)
                {
                    var parentCheck = await CheckAsync(userId, pParts[0], pParts[1], "view");
                    if (parentCheck.Allowed)
                        return new RebacDecision(true,
                            $"Inherited view via parent '{parentSubject}'.",
                            "parent");
                }
            }
        }

        return new RebacDecision(false, $"No relation grants '{permission}' on {objectType}:{objectId} for user:{userId}.");
    }
}

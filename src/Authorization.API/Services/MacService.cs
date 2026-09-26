using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Services;

/// <summary>
/// Mandatory Access Control (MAC) decision service.
/// Implements Bell-LaPadula model (confidentiality):
///   - Simple Security Property (no read up): subject.clearance >= object.classification
///   - *-Property (no write down): subject.clearance <= object.classification
/// Decisions are enforced by the system; resource owners cannot override.
/// </summary>
public record MacDecision(
    bool Allowed,
    string Operation,
    string Reason,
    string SubjectClearance,
    string ResourceClassification
);

public interface IMacService
{
    Task<MacDecision> CanReadAsync(ApplicationUser subject, Guid resourceId);
    Task<MacDecision> CanWriteAsync(ApplicationUser subject, Guid resourceId);
    Task<Resource?> GetResourceAsync(Guid resourceId);
    MacDecision Evaluate(ApplicationUser subject, Resource resource, string operation);
}

public class MacService : IMacService
{
    private readonly ApplicationDbContext _db;

    public MacService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Resource?> GetResourceAsync(Guid resourceId)
    {
        return await _db.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);
    }

    public async Task<MacDecision> CanReadAsync(ApplicationUser subject, Guid resourceId)
    {
        var resource = await GetResourceAsync(resourceId);
        if (resource == null)
        {
            return new MacDecision(false, "Read", "Resource not found or inactive.",
                subject.ClearanceLevel ?? "None", "Unknown");
        }
        return Evaluate(subject, resource, "Read");
    }

    public async Task<MacDecision> CanWriteAsync(ApplicationUser subject, Guid resourceId)
    {
        var resource = await GetResourceAsync(resourceId);
        if (resource == null)
        {
            return new MacDecision(false, "Write", "Resource not found or inactive.",
                subject.ClearanceLevel ?? "None", "Unknown");
        }
        return Evaluate(subject, resource, "Write");
    }

    /// <summary>
    /// Core MAC evaluation (Bell-LaPadula).
    /// </summary>
    public MacDecision Evaluate(ApplicationUser subject, Resource resource, string operation)
    {
        var subjectClearance = subject.ClearanceLevel ?? SecurityLevel.Public;
        var resourceClass = resource.Sensitivity ?? SecurityLevel.Public;

        var subjectRank = SecurityLevel.GetRank(subjectClearance);
        var resourceRank = SecurityLevel.GetRank(resourceClass);

        if (operation.Equals("Read", StringComparison.OrdinalIgnoreCase))
        {
            // No Read Up
            if (subjectRank >= resourceRank)
            {
                return new MacDecision(
                    true,
                    "Read",
                    $"MAC Allow (Read): subject clearance '{subjectClearance}' (rank {subjectRank}) >= resource classification '{resourceClass}' (rank {resourceRank}).",
                    subjectClearance,
                    resourceClass
                );
            }

            return new MacDecision(
                false,
                "Read",
                $"MAC Deny (Read Up violation): subject clearance '{subjectClearance}' (rank {subjectRank}) < resource classification '{resourceClass}' (rank {resourceRank}).",
                subjectClearance,
                resourceClass
            );
        }

        if (operation.Equals("Write", StringComparison.OrdinalIgnoreCase))
        {
            // No Write Down (*-property)
            if (subjectRank <= resourceRank)
            {
                return new MacDecision(
                    true,
                    "Write",
                    $"MAC Allow (Write): subject clearance '{subjectClearance}' (rank {subjectRank}) <= resource classification '{resourceClass}' (rank {resourceRank}).",
                    subjectClearance,
                    resourceClass
                );
            }

            return new MacDecision(
                false,
                "Write",
                $"MAC Deny (Write Down violation): subject clearance '{subjectClearance}' (rank {subjectRank}) > resource classification '{resourceClass}' (rank {resourceRank}).",
                subjectClearance,
                resourceClass
            );
        }

        return new MacDecision(
            false,
            operation,
            $"MAC Deny: unsupported operation '{operation}'. Only Read and Write are defined.",
            subjectClearance,
            resourceClass
        );
    }
}

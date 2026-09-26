namespace Authorization.API.Models;

/// <summary>
/// Relationship-Based Access Control (ReBAC) tuple – Zanzibar style.
/// Format: object#relation@subject
/// Examples:
///   document:123#owner@user:alice
///   document:123#editor@user:bob
///   document:123#viewer@group:eng#member
///   folder:1#parent@folder:2  (hierarchy)
/// </summary>
public class RelationTuple
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>e.g. document:guid or folder:guid or group:eng</summary>
    public string ObjectType { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>e.g. owner, editor, viewer, member, parent</summary>
    public string Relation { get; set; } = string.Empty;

    /// <summary>
    /// Subject can be:
    /// - user:{userId}
    /// - group:{groupId}#member  (userset)
    /// - {objectType}:{objectId}#{relation}  (userset rewrite)
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

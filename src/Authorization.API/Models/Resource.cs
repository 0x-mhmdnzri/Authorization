namespace Authorization.API.Models;

/// <summary>
/// Resource protected by ABAC. Attributes of the resource are used in policy evaluation.
/// </summary>
public class Resource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty; // e.g. Document, Report, Budget
    public string? OwnerDepartment { get; set; }
    public string Sensitivity { get; set; } = "Internal"; // Public, Internal, Confidential, Restricted
    public string? OwnerId { get; set; } // ApplicationUser.Id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Extra flexible attributes (JSON stored as string for simplicity)
    public string? AttributesJson { get; set; }
}

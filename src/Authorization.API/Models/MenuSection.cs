namespace Authorization.API.Models;

/// <summary>
/// Admin panel menu section. Served by GET /api/menu (SSR-ready).
/// AuthorizationMethod labels which model(s) protect this section.
/// </summary>
public class MenuSection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable key used in permission checks, e.g. "users", "rbac", "abac".</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Href { get; set; }
    public string? Icon { get; set; }

    /// <summary>Comma-separated method labels shown in UI, e.g. "RBAC,DAC".</summary>
    public string AuthorizationMethods { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Parent section key for nested menus (optional).</summary>
    public string? ParentKey { get; set; }

    public ICollection<SectionPermission> Permissions { get; set; } = new List<SectionPermission>();
}

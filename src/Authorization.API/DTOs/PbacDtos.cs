using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreatePolicyRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ResourceType { get; set; } = "*";
    public string Action { get; set; } = "*";
    public string Effect { get; set; } = "Allow";
    public int Priority { get; set; } = 100;

    public string? RequiredRoles { get; set; }
    public string? RequiredDepartments { get; set; }
    public string? MinimumClearance { get; set; }
    public bool RequireSameDepartment { get; set; }
    public bool RequireBusinessHours { get; set; }
    public string? MaxResourceSensitivity { get; set; }
    public bool RequireDacGrant { get; set; }
}

public class PbacEvaluateRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string Action { get; set; } = "Read";
}

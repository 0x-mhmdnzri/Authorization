using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreateResourceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    public string? OwnerDepartment { get; set; }

    [Required]
    public string Sensitivity { get; set; } = "Internal"; // Public, Internal, Confidential, Restricted, Secret, TopSecret
}

public class CreateAbacPolicyRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ResourceType { get; set; }

    [Required]
    public string Action { get; set; } = "Read";

    [Required]
    public string Effect { get; set; } = "Allow"; // Allow | Deny

    public int Priority { get; set; } = 100;

    public bool RequireSameDepartment { get; set; }
    public bool RequireBusinessHours { get; set; }
    public string? MinimumClearance { get; set; }
    public string? AllowedDepartments { get; set; }
    public string? RequiredSensitivityMax { get; set; }
}

public class EvaluateAccessRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string Action { get; set; } = "Read";
}

public class EvaluateAccessResponse
{
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? MatchedPolicy { get; set; }
    public object? SubjectAttributes { get; set; }
    public object? ResourceAttributes { get; set; }
}

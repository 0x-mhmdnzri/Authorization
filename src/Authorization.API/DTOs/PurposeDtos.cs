using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreatePurposeRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class AssignPurposeRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public Guid PurposeId { get; set; }

    public string? AllowedRoles { get; set; }
    public bool RequiresExplicitConsent { get; set; }
}

public class PurposeEvaluateRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string PurposeCode { get; set; } = string.Empty;

    public string Action { get; set; } = "Read";
}

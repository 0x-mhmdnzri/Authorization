using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreatePrivilegeDefinitionRequest
{
    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DefaultDurationMinutes { get; set; } = 60;
    public int MaxDurationMinutes { get; set; } = 480;
    public string? AllowedRequesterRoles { get; set; }
    public string? ApproverRoles { get; set; }
    public bool RequiresApproval { get; set; } = true;
}

public class RequestElevationRequest
{
    [Required]
    public Guid PrivilegeId { get; set; }

    [Required, MaxLength(1000)]
    public string Justification { get; set; } = string.Empty;

    public int DurationMinutes { get; set; } = 60;
}

public class DecideElevationRequest
{
    [Required]
    public Guid RequestId { get; set; }
    public bool Approve { get; set; }
    public string? Note { get; set; }
}

public class PrivilegedActionRequest
{
    [Required]
    public string PrivilegeCode { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? Resource { get; set; }
    public string? Details { get; set; }
}

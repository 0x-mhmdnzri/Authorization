using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class GrantAccessRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string SubjectId { get; set; } = string.Empty; // user id to grant

    /// <summary>
    /// Comma-separated: Read, Write, Delete, Share  or "Full"
    /// </summary>
    [Required]
    public string Permissions { get; set; } = "Read";

    public DateTime? ExpiresAt { get; set; }
}

public class RevokeAccessRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string SubjectId { get; set; } = string.Empty;
}

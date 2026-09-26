using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class WriteTupleRequest
{
    [Required]
    public string ObjectType { get; set; } = string.Empty; // document, folder, group

    [Required]
    public string ObjectId { get; set; } = string.Empty;

    [Required]
    public string Relation { get; set; } = string.Empty; // owner, editor, viewer, member, parent

    [Required]
    public string Subject { get; set; } = string.Empty; // user:{id} | group:{id}#member
}

public class RebacCheckRequest
{
    [Required]
    public string ObjectType { get; set; } = string.Empty;

    [Required]
    public string ObjectId { get; set; } = string.Empty;

    [Required]
    public string Permission { get; set; } = "view"; // view, edit, share, owner
}

public class DeleteTupleRequest
{
    [Required]
    public string ObjectType { get; set; } = string.Empty;
    [Required]
    public string ObjectId { get; set; } = string.Empty;
    [Required]
    public string Relation { get; set; } = string.Empty;
    [Required]
    public string Subject { get; set; } = string.Empty;
}

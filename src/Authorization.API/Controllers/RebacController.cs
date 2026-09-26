using Authorization.API.Data;
using Authorization.API.DTOs;
using Authorization.API.Models;
using Authorization.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Controllers;

/// <summary>
/// Relationship-Based Access Control (ReBAC) – Zanzibar-style tuples.
/// Access is determined by relations between subjects and objects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RebacController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRebacService _rebacService;
    private readonly ApplicationDbContext _db;

    public RebacController(
        UserManager<ApplicationUser> userManager,
        IRebacService rebacService,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _rebacService = rebacService;
        _db = db;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Write a relation tuple (e.g. document:123#editor@user:alice).
    /// </summary>
    [HttpPost("tuples")]
    public async Task<IActionResult> WriteTuple([FromBody] WriteTupleRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        await _rebacService.WriteTupleAsync(
            request.ObjectType.ToLowerInvariant(),
            request.ObjectId,
            request.Relation.ToLowerInvariant(),
            request.Subject,
            user.Id);

        return Ok(new
        {
            message = "Tuple written.",
            tuple = $"{request.ObjectType}:{request.ObjectId}#{request.Relation}@{request.Subject}"
        });
    }

    /// <summary>
    /// Delete a relation tuple.
    /// </summary>
    [HttpDelete("tuples")]
    public async Task<IActionResult> DeleteTuple([FromBody] DeleteTupleRequest request)
    {
        var ok = await _rebacService.DeleteTupleAsync(
            request.ObjectType.ToLowerInvariant(),
            request.ObjectId,
            request.Relation.ToLowerInvariant(),
            request.Subject);

        if (!ok) return NotFound(new { message = "Tuple not found." });
        return Ok(new { message = "Tuple deleted." });
    }

    /// <summary>
    /// List all tuples for an object.
    /// </summary>
    [HttpGet("tuples/{objectType}/{objectId}")]
    public async Task<IActionResult> ListTuples(string objectType, string objectId)
    {
        var list = await _rebacService.ListTuplesAsync(objectType.ToLowerInvariant(), objectId);
        return Ok(list.Select(t => new
        {
            t.Id,
            tuple = $"{t.ObjectType}:{t.ObjectId}#{t.Relation}@{t.Subject}",
            t.ObjectType,
            t.ObjectId,
            t.Relation,
            t.Subject,
            t.CreatedAt
        }));
    }

    /// <summary>
    /// Check whether current user has a permission via relations.
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] RebacCheckRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var decision = await _rebacService.CheckAsync(
            user.Id,
            request.ObjectType.ToLowerInvariant(),
            request.ObjectId,
            request.Permission.ToLowerInvariant());

        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            matchedRelation = decision.MatchedRelation,
            model = "ReBAC"
        });
    }

    /// <summary>
    /// Access resource content via ReBAC (object type = document, object id = resource guid).
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, [FromQuery] string permission = "view")
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (resource == null) return NotFound();

        var decision = await _rebacService.CheckAsync(user.Id, "document", id.ToString(), permission);

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Relationship-Based Access Control.",
                reason = decision.Reason
            });
        }

        return Ok(new
        {
            message = "Access granted via ReBAC.",
            reason = decision.Reason,
            matchedRelation = decision.MatchedRelation,
            content = $"ReBAC-protected content of '{resource.Name}'."
        });
    }

    /// <summary>
    /// Convenience: share a document resource with a user (writes editor/viewer tuple).
    /// Only current owner (or admin) should call this in production; demo is open to authenticated users.
    /// </summary>
    [HttpPost("share")]
    public async Task<IActionResult> Share(
        [FromQuery] Guid resourceId,
        [FromQuery] string targetUserId,
        [FromQuery] string relation = "viewer")
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        // Optional: only owner can share
        var isOwner = await _rebacService.CheckAsync(user.Id, "document", resourceId.ToString(), "owner");
        if (!isOwner.Allowed && resource.OwnerId != user.Id)
            return Forbid();

        await _rebacService.WriteTupleAsync(
            "document",
            resourceId.ToString(),
            relation.ToLowerInvariant(),
            $"user:{targetUserId}",
            user.Id);

        return Ok(new
        {
            message = $"Shared as {relation}.",
            tuple = $"document:{resourceId}#{relation}@user:{targetUserId}"
        });
    }
}

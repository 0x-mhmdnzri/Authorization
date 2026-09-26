using Authorization.API.DTOs;
using Authorization.API.Models;
using Authorization.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.API.Controllers;

/// <summary>
/// Discretionary Access Control (DAC) endpoints.
/// Resource owner decides who can access the resource (classic ACL model).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DacController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDacService _dacService;

    public DacController(UserManager<ApplicationUser> userManager, IDacService dacService)
    {
        _userManager = userManager;
        _dacService = dacService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Grant access to a user on a resource (owner or user with Share permission).
    /// Classic DAC: owner exercises discretion.
    /// </summary>
    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] GrantAccessRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var entry = await _dacService.GrantAsync(
            user.Id, request.ResourceId, request.SubjectId, request.Permissions, request.ExpiresAt);

        if (entry == null)
            return Forbid(); // or BadRequest – not owner / no Share right

        return Ok(new
        {
            message = "Access granted (DAC).",
            entry.Id,
            entry.ResourceId,
            entry.SubjectId,
            entry.Permissions,
            entry.GrantedAt,
            entry.ExpiresAt
        });
    }

    /// <summary>
    /// Revoke access (only resource owner).
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeAccessRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var ok = await _dacService.RevokeAsync(user.Id, request.ResourceId, request.SubjectId);
        if (!ok)
            return BadRequest(new { message = "Revoke failed. Only the owner can revoke, or entry not found." });

        return Ok(new { message = "Access revoked." });
    }

    /// <summary>
    /// List ACL entries for a resource.
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/acl")]
    public async Task<IActionResult> GetAcl(Guid resourceId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        // Only owner can view full ACL in this simple model
        var resource = await _dacService.GetResourceAsync(resourceId);
        if (resource == null) return NotFound();
        if (resource.OwnerId != user.Id)
            return Forbid();

        var acl = await _dacService.GetAclAsync(resourceId);
        return Ok(acl.Select(e => new
        {
            e.Id,
            e.SubjectId,
            e.Permissions,
            e.GrantedById,
            e.GrantedAt,
            e.ExpiresAt
        }));
    }

    /// <summary>
    /// Evaluate whether current user can perform an action (DAC check).
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateAccessRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var decision = await _dacService.CanAccessAsync(user.Id, request.ResourceId, request.Action);
        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            isOwner = decision.IsOwner,
            model = "DAC"
        });
    }

    /// <summary>
    /// Access resource content under DAC rules.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var decision = await _dacService.CanAccessAsync(user.Id, id, "Read");
        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Discretionary Access Control.",
                reason = decision.Reason
            });
        }

        var resource = await _dacService.GetResourceAsync(id);
        return Ok(new
        {
            message = decision.IsOwner ? "Access granted as owner." : "Access granted via ACL.",
            reason = decision.Reason,
            resource = new { resource!.Id, resource.Name, resource.ResourceType, resource.OwnerId },
            content = $"DAC-protected content of '{resource.Name}'."
        });
    }
}

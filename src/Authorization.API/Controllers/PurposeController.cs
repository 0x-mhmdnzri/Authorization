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
/// Purpose-Based Access Control.
/// Access is granted only when the caller states an allowed purpose for the resource.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurposeController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPurposeService _purposeService;

    public PurposeController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPurposeService purposeService)
    {
        _db = db;
        _userManager = userManager;
        _purposeService = purposeService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Create a purpose definition (Admin).
    /// </summary>
    [HttpPost("purposes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePurpose([FromBody] CreatePurposeRequest request)
    {
        if (await _db.Purposes.AnyAsync(p => p.Code == request.Code))
            return BadRequest(new { message = $"Purpose code '{request.Code}' already exists." });

        var purpose = new Purpose
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description
        };
        _db.Purposes.Add(purpose);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPurposes), new { id = purpose.Id }, purpose);
    }

    /// <summary>
    /// List all active purposes.
    /// </summary>
    [HttpGet("purposes")]
    public async Task<IActionResult> GetPurposes()
    {
        var list = await _purposeService.GetPurposesAsync();
        return Ok(list);
    }

    /// <summary>
    /// Assign an allowed purpose to a resource (Admin / owner).
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignPurpose([FromBody] AssignPurposeRequest request)
    {
        var resource = await _db.Resources.FindAsync(request.ResourceId);
        if (resource == null) return NotFound(new { message = "Resource not found." });

        var purpose = await _db.Purposes.FindAsync(request.PurposeId);
        if (purpose == null || !purpose.IsActive)
            return BadRequest(new { message = "Purpose not found or inactive." });

        if (await _db.ResourcePurposes.AnyAsync(rp => rp.ResourceId == request.ResourceId && rp.PurposeId == request.PurposeId))
            return BadRequest(new { message = "Purpose already assigned to this resource." });

        var link = new ResourcePurpose
        {
            ResourceId = request.ResourceId,
            PurposeId = request.PurposeId,
            AllowedRoles = request.AllowedRoles,
            RequiresExplicitConsent = request.RequiresExplicitConsent
        };
        _db.ResourcePurposes.Add(link);
        await _db.SaveChangesAsync();
        return Ok(link);
    }

    /// <summary>
    /// List purposes allowed on a resource.
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/purposes")]
    public async Task<IActionResult> GetResourcePurposes(Guid resourceId)
    {
        var list = await _purposeService.GetResourcePurposesAsync(resourceId);
        return Ok(list.Select(rp => new
        {
            rp.Id,
            rp.ResourceId,
            PurposeCode = rp.Purpose.Code,
            PurposeName = rp.Purpose.Name,
            rp.AllowedRoles,
            rp.RequiresExplicitConsent
        }));
    }

    /// <summary>
    /// Evaluate access for a stated purpose (core of Purpose-Based AC).
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] PurposeEvaluateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var decision = await _purposeService.EvaluateAsync(
            user.Id, roles, request.ResourceId, request.PurposeCode.ToUpperInvariant(), request.Action);

        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            purposeCode = decision.PurposeCode,
            model = "PBAC-Purpose"
        });
    }

    /// <summary>
    /// Access resource content only when a valid purpose is provided.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, [FromQuery] string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            return BadRequest(new { message = "Query parameter 'purpose' is required." });

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var decision = await _purposeService.EvaluateAsync(user.Id, roles, id, purpose.ToUpperInvariant());

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Purpose-Based Access Control.",
                reason = decision.Reason,
                purposeCode = decision.PurposeCode
            });
        }

        var resource = await _db.Resources.AsNoTracking().FirstAsync(r => r.Id == id);
        return Ok(new
        {
            message = "Access granted for the stated purpose.",
            reason = decision.Reason,
            purposeCode = decision.PurposeCode,
            content = $"Purpose-protected content of '{resource.Name}' (purpose: {decision.PurposeCode})."
        });
    }

    /// <summary>
    /// Recent purpose access logs (Admin).
    /// </summary>
    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLogs([FromQuery] int take = 50)
    {
        var logs = await _db.PurposeAccessLogs.AsNoTracking()
            .OrderByDescending(l => l.AccessedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
        return Ok(logs);
    }
}

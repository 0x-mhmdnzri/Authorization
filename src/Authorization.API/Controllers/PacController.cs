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
/// Privileged Access Control (PAC) – Just-In-Time elevation, no standing privileges.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPacService _pacService;

    public PacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPacService pacService)
    {
        _db = db;
        _userManager = userManager;
        _pacService = pacService;
    }

    private async Task<(ApplicationUser? User, IList<string> Roles)> GetCurrentAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return (null, new List<string>());
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (null, new List<string>());
        var roles = await _userManager.GetRolesAsync(user);
        return (user, roles);
    }

    /// <summary>
    /// Define a privileged capability (Admin).
    /// </summary>
    [HttpPost("privileges")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePrivilege([FromBody] CreatePrivilegeDefinitionRequest request)
    {
        if (await _db.PrivilegeDefinitions.AnyAsync(p => p.Code == request.Code))
            return BadRequest(new { message = "Privilege code already exists." });

        var priv = new PrivilegeDefinition
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
            MaxDurationMinutes = request.MaxDurationMinutes,
            AllowedRequesterRoles = request.AllowedRequesterRoles,
            ApproverRoles = request.ApproverRoles,
            RequiresApproval = request.RequiresApproval
        };
        _db.PrivilegeDefinitions.Add(priv);
        await _db.SaveChangesAsync();
        return Ok(priv);
    }

    [HttpGet("privileges")]
    public async Task<IActionResult> ListPrivileges()
    {
        var list = await _db.PrivilegeDefinitions.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Request JIT elevation.
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> RequestElevation([FromBody] RequestElevationRequest request)
    {
        var (user, roles) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var result = await _pacService.RequestElevationAsync(
            user.Id, roles, request.PrivilegeId, request.Justification, request.DurationMinutes);

        if (result == null)
            return BadRequest(new { message = "Cannot request this privilege (not found or role not allowed)." });

        return Ok(new
        {
            result.Id,
            result.Status,
            result.RequestedDurationMinutes,
            result.ExpiresAt,
            message = result.Status == ElevationStatus.Active
                ? "Elevation active (no approval required)."
                : "Elevation request pending approval."
        });
    }

    /// <summary>
    /// Approve or deny a pending request (Admin / approver roles).
    /// </summary>
    [HttpPost("decide")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Decide([FromBody] DecideElevationRequest request)
    {
        var (user, roles) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var result = await _pacService.DecideAsync(
            request.RequestId, user.Id, roles, request.Approve, request.Note);

        if (result == null)
            return BadRequest(new { message = "Cannot decide (not found, not pending, or not authorized)." });

        return Ok(new
        {
            result.Id,
            result.Status,
            result.ExpiresAt,
            result.ApproverNote
        });
    }

    /// <summary>
    /// List my active elevations.
    /// </summary>
    [HttpGet("my-elevations")]
    public async Task<IActionResult> MyElevations()
    {
        var (user, _) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var list = await _pacService.GetActiveElevationsAsync(user.Id);
        return Ok(list.Select(r => new
        {
            r.Id,
            privilegeCode = r.Privilege.Code,
            privilegeName = r.Privilege.Name,
            r.Status,
            r.StartsAt,
            r.ExpiresAt,
            r.Justification
        }));
    }

    /// <summary>
    /// Check if current user currently holds a privilege.
    /// </summary>
    [HttpGet("check/{privilegeCode}")]
    public async Task<IActionResult> Check(string privilegeCode)
    {
        var (user, _) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var has = await _pacService.HasActivePrivilegeAsync(user.Id, privilegeCode.ToUpperInvariant());
        return Ok(new { privilegeCode = privilegeCode.ToUpperInvariant(), hasActiveElevation = has });
    }

    /// <summary>
    /// Perform a privileged action (only if elevation is active). Logs the action.
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] PrivilegedActionRequest request)
    {
        var (user, _) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var code = request.PrivilegeCode.ToUpperInvariant();
        var has = await _pacService.HasActivePrivilegeAsync(user.Id, code);
        if (!has)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "No active elevation for this privilege. Request JIT access first.",
                privilegeCode = code
            });
        }

        var elevation = (await _pacService.GetActiveElevationsAsync(user.Id))
            .First(e => e.Privilege.Code == code);

        await _pacService.LogActionAsync(
            user.Id, elevation.Id, code, request.Action, request.Resource, request.Details);

        return Ok(new
        {
            message = "Privileged action executed and logged.",
            privilegeCode = code,
            action = request.Action,
            resource = request.Resource,
            elevationExpiresAt = elevation.ExpiresAt
        });
    }

    /// <summary>
    /// Revoke an elevation (self or Admin).
    /// </summary>
    [HttpPost("revoke/{requestId:guid}")]
    public async Task<IActionResult> Revoke(Guid requestId)
    {
        var (user, roles) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var req = await _db.PrivilegeElevationRequests.FindAsync(requestId);
        if (req == null) return NotFound();

        if (req.RequesterId != user.Id && !roles.Contains("Admin"))
            return Forbid();

        await _pacService.RevokeAsync(requestId, user.Id);
        return Ok(new { message = "Elevation revoked." });
    }

    /// <summary>
    /// Pending requests (approvers).
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Pending()
    {
        var list = await _db.PrivilegeElevationRequests.AsNoTracking()
            .Include(r => r.Privilege)
            .Where(r => r.Status == ElevationStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .Select(r => new
            {
                r.Id,
                r.RequesterId,
                privilegeCode = r.Privilege.Code,
                r.Justification,
                r.RequestedDurationMinutes,
                r.RequestedAt
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Privileged action audit log (Admin).
    /// </summary>
    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Logs([FromQuery] int take = 50)
    {
        var logs = await _db.PrivilegedActionLogs.AsNoTracking()
            .OrderByDescending(l => l.PerformedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
        return Ok(logs);
    }
}

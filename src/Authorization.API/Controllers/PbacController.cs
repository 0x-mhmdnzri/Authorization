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
/// Policy-Based Access Control (PBAC) – central Policy Decision Point.
/// Policies are centrally managed and can combine roles, attributes, context and DAC grants.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PbacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPbacService _pbacService;
    private readonly IDacService _dacService;

    public PbacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPbacService pbacService,
        IDacService dacService)
    {
        _db = db;
        _userManager = userManager;
        _pbacService = pbacService;
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
    /// Create a central PBAC policy (Admin only).
    /// </summary>
    [HttpPost("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        var user = await GetCurrentUserAsync();
        var policy = new Policy
        {
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Action = request.Action,
            Effect = request.Effect,
            Priority = request.Priority,
            RequiredRoles = request.RequiredRoles,
            RequiredDepartments = request.RequiredDepartments,
            MinimumClearance = request.MinimumClearance,
            RequireSameDepartment = request.RequireSameDepartment,
            RequireBusinessHours = request.RequireBusinessHours,
            MaxResourceSensitivity = request.MaxResourceSensitivity,
            RequireDacGrant = request.RequireDacGrant,
            CreatedBy = user?.Id
        };

        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPolicies), new { id = policy.Id }, policy);
    }

    /// <summary>
    /// List all enabled PBAC policies.
    /// </summary>
    [HttpGet("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPolicies()
    {
        var policies = await _pbacService.GetPoliciesAsync();
        return Ok(policies);
    }

    /// <summary>
    /// Central evaluate endpoint – Policy Decision Point.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] PbacEvaluateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId && r.IsActive);
        if (resource == null)
            return NotFound(new { message = "Resource not found." });

        var roles = await _userManager.GetRolesAsync(user);
        var decision = await _pbacService.EvaluateAsync(user, roles, resource, request.Action);

        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            matchedPolicy = decision.MatchedPolicyName,
            priority = decision.MatchedPriority,
            model = "PBAC",
            subject = new { user.Id, user.Email, user.Department, user.ClearanceLevel, roles },
            resource = new { resource.Id, resource.Name, resource.ResourceType, resource.Sensitivity, resource.OwnerDepartment }
        });
    }

    /// <summary>
    /// Access resource content through the central PBAC layer.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (resource == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var decision = await _pbacService.EvaluateAsync(user, roles, resource, "Read");

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Policy-Based Access Control.",
                reason = decision.Reason,
                matchedPolicy = decision.MatchedPolicyName
            });
        }

        return Ok(new
        {
            message = "Access granted by PBAC.",
            reason = decision.Reason,
            matchedPolicy = decision.MatchedPolicyName,
            content = $"PBAC-protected content of '{resource.Name}'."
        });
    }
}

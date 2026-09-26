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
/// Attribute-Based Access Control (ABAC) endpoints.
/// Decisions are made by evaluating attributes of Subject, Resource, Action and Environment.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AbacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAbacService _abacService;

    public AbacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAbacService abacService)
    {
        _db = db;
        _userManager = userManager;
        _abacService = abacService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    // -------------------- Resources --------------------

    /// <summary>
    /// Create a new protected resource (Admin only).
    /// </summary>
    [HttpPost("resources")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = new Resource
        {
            Name = request.Name,
            ResourceType = request.ResourceType,
            OwnerDepartment = request.OwnerDepartment ?? user.Department,
            Sensitivity = request.Sensitivity,
            OwnerId = user.Id
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
    }

    /// <summary>
    /// List all resources.
    /// </summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources()
    {
        var resources = await _db.Resources
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.ResourceType,
                r.OwnerDepartment,
                r.Sensitivity,
                r.OwnerId,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(resources);
    }

    /// <summary>
    /// Get a single resource.
    /// </summary>
    [HttpGet("resources/{id:guid}")]
    public async Task<IActionResult> GetResource(Guid id)
    {
        var resource = await _abacService.GetResourceAsync(id);
        if (resource == null) return NotFound();
        return Ok(resource);
    }

    // -------------------- Policies --------------------

    /// <summary>
    /// Create a new ABAC policy (Admin only).
    /// </summary>
    [HttpPost("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateAbacPolicyRequest request)
    {
        var policy = new AbacPolicy
        {
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Action = request.Action,
            Effect = request.Effect,
            Priority = request.Priority,
            RequireSameDepartment = request.RequireSameDepartment,
            RequireBusinessHours = request.RequireBusinessHours,
            MinimumClearance = request.MinimumClearance,
            AllowedDepartments = request.AllowedDepartments,
            RequiredSensitivityMax = request.RequiredSensitivityMax
        };

        _db.AbacPolicies.Add(policy);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPolicies), new { id = policy.Id }, policy);
    }

    /// <summary>
    /// List all enabled ABAC policies.
    /// </summary>
    [HttpGet("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPolicies()
    {
        var policies = await _abacService.GetPoliciesAsync();
        return Ok(policies);
    }

    // -------------------- Evaluation (core of ABAC) --------------------

    /// <summary>
    /// Evaluate whether the current user can perform the given action on a resource.
    /// This is the heart of ABAC: attributes of subject + resource + environment are checked against policies.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateAccessRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _abacService.GetResourceAsync(request.ResourceId);
        if (resource == null)
            return NotFound(new { message = "Resource not found." });

        var context = new AbacEvaluationContext(
            Subject: user,
            Resource: resource,
            Action: request.Action,
            EvaluationTimeUtc: DateTime.UtcNow
        );

        var decision = await _abacService.EvaluateAsync(context);

        return Ok(new EvaluateAccessResponse
        {
            Allowed = decision.Allowed,
            Reason = decision.Reason,
            MatchedPolicy = decision.MatchedPolicyName,
            SubjectAttributes = new
            {
                user.Id,
                user.Email,
                user.Department,
                user.ClearanceLevel
            },
            ResourceAttributes = new
            {
                resource.Id,
                resource.Name,
                resource.ResourceType,
                resource.OwnerDepartment,
                resource.Sensitivity
            }
        });
    }

    /// <summary>
    /// Demo endpoint: try to "read" a resource. Returns 403 if ABAC denies.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetResourceContent(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _abacService.GetResourceAsync(id);
        if (resource == null) return NotFound();

        var context = new AbacEvaluationContext(user, resource, "Read", DateTime.UtcNow);
        var decision = await _abacService.EvaluateAsync(context);

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by ABAC policy.",
                reason = decision.Reason,
                matchedPolicy = decision.MatchedPolicyName
            });
        }

        return Ok(new
        {
            message = "Access granted by ABAC.",
            reason = decision.Reason,
            matchedPolicy = decision.MatchedPolicyName,
            content = $"This is the protected content of resource '{resource.Name}' (Sensitivity: {resource.Sensitivity})."
        });
    }
}

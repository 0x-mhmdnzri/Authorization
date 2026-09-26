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
/// Context-Based Access Control (CBAC).
/// Decisions depend on environmental context: time, network, device, location, auth strength.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CbacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICbacService _cbacService;

    public CbacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ICbacService cbacService)
    {
        _db = db;
        _userManager = userManager;
        _cbacService = cbacService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    [HttpPost("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateContextPolicyRequest request)
    {
        var policy = new ContextPolicy
        {
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Action = request.Action,
            Effect = request.Effect,
            Priority = request.Priority,
            AllowedHourStart = request.AllowedHourStart,
            AllowedHourEnd = request.AllowedHourEnd,
            AllowedDaysOfWeek = request.AllowedDaysOfWeek,
            RequireTrustedNetwork = request.RequireTrustedNetwork,
            RequireManagedDevice = request.RequireManagedDevice,
            AllowedCountries = request.AllowedCountries,
            BlockedCountries = request.BlockedCountries,
            MinimumAuthMethod = request.MinimumAuthMethod,
            BlockAnomalousLocation = request.BlockAnomalousLocation
        };
        _db.ContextPolicies.Add(policy);
        await _db.SaveChangesAsync();
        return Ok(policy);
    }

    [HttpGet("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPolicies()
    {
        return Ok(await _cbacService.GetPoliciesAsync());
    }

    /// <summary>
    /// Evaluate access under current context.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] CbacEvaluateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        string? resourceType = request.ResourceType;
        if (request.ResourceId.HasValue)
        {
            var res = await _db.Resources.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ResourceId.Value);
            if (res != null) resourceType = res.ResourceType;
        }

        var ctx = new AccessContext
        {
            IsTrustedNetwork = request.IsTrustedNetwork,
            IsManagedDevice = request.IsManagedDevice,
            CountryCode = request.CountryCode,
            IsAnomalousLocation = request.IsAnomalousLocation,
            AuthMethod = request.AuthMethod,
            IpAddress = request.IpAddress,
            DeviceId = request.DeviceId
        };

        var decision = await _cbacService.EvaluateAsync(
            user.Id, resourceType, request.Action, ctx, request.ResourceId);

        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            matchedPolicy = decision.MatchedPolicyName,
            model = "CBAC",
            context = new
            {
                ctx.EvaluationTimeUtc,
                ctx.IsTrustedNetwork,
                ctx.IsManagedDevice,
                ctx.CountryCode,
                ctx.AuthMethod
            }
        });
    }

    /// <summary>
    /// Access resource content; context supplied via query params.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(
        Guid id,
        [FromQuery] bool trustedNetwork = false,
        [FromQuery] bool managedDevice = false,
        [FromQuery] string? country = null,
        [FromQuery] string authMethod = "Password",
        [FromQuery] bool anomalousLocation = false)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (resource == null) return NotFound();

        var ctx = new AccessContext
        {
            IsTrustedNetwork = trustedNetwork,
            IsManagedDevice = managedDevice,
            CountryCode = country,
            AuthMethod = authMethod,
            IsAnomalousLocation = anomalousLocation
        };

        var decision = await _cbacService.EvaluateAsync(
            user.Id, resource.ResourceType, "Read", ctx, id);

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Context-Based Access Control.",
                reason = decision.Reason,
                matchedPolicy = decision.MatchedPolicyName
            });
        }

        return Ok(new
        {
            message = "Access granted by CBAC.",
            reason = decision.Reason,
            matchedPolicy = decision.MatchedPolicyName,
            content = $"CBAC-protected content of '{resource.Name}'."
        });
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Logs([FromQuery] int take = 50)
    {
        var logs = await _db.ContextEvaluationLogs.AsNoTracking()
            .OrderByDescending(l => l.EvaluatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
        return Ok(logs);
    }
}

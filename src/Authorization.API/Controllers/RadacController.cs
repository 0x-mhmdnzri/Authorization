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
/// Risk-Adaptive Access Control (RAdAC).
/// Access decisions adapt based on real-time risk vs operational need.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RadacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRadacService _radacService;

    public RadacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IRadacService radacService)
    {
        _db = db;
        _userManager = userManager;
        _radacService = radacService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Create / update risk policy thresholds (Admin).
    /// </summary>
    [HttpPost("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateRiskPolicyRequest request)
    {
        var policy = new RiskPolicy
        {
            Name = request.Name,
            Description = request.Description,
            NormalRiskThreshold = request.NormalRiskThreshold,
            MaxAcceptableRisk = request.MaxAcceptableRisk,
            CriticalNeedThreshold = request.CriticalNeedThreshold
        };
        _db.RiskPolicies.Add(policy);
        await _db.SaveChangesAsync();
        return Ok(policy);
    }

    [HttpGet("policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPolicies()
    {
        return Ok(await _radacService.GetPoliciesAsync());
    }

    /// <summary>
    /// Core RAdAC evaluation.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] RadacEvaluateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var ctx = new RiskContext
        {
            IpAddress = request.IpAddress,
            DeviceId = request.DeviceId,
            IsManagedDevice = request.IsManagedDevice,
            IsTrustedNetwork = request.IsTrustedNetwork,
            CountryCode = request.CountryCode,
            AuthMethod = request.AuthMethod,
            FailedLoginsLastHour = request.FailedLoginsLastHour,
            IsAnomalousLocation = request.IsAnomalousLocation,
            IsOffHours = request.IsOffHours,
            OperationalNeed = request.OperationalNeed
        };

        var decision = await _radacService.EvaluateAsync(user, request.ResourceId, request.Action, ctx);

        return Ok(new
        {
            allowed = decision.Allowed,
            decision = decision.Decision,
            riskScore = decision.RiskScore,
            operationalNeed = decision.OperationalNeedScore,
            reason = decision.Reason,
            factors = decision.FactorBreakdown,
            model = "RAdAC"
        });
    }

    /// <summary>
    /// Access resource content under RAdAC.
    /// Query/body can supply risk context; defaults are conservative.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(
        Guid id,
        [FromQuery] bool managedDevice = false,
        [FromQuery] bool trustedNetwork = false,
        [FromQuery] string authMethod = "Password",
        [FromQuery] double operationalNeed = 50)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (resource == null) return NotFound();

        var ctx = new RiskContext
        {
            IsManagedDevice = managedDevice,
            IsTrustedNetwork = trustedNetwork,
            AuthMethod = authMethod,
            OperationalNeed = operationalNeed,
            IsOffHours = DateTime.UtcNow.Hour < 6 || DateTime.UtcNow.Hour >= 22
        };

        var decision = await _radacService.EvaluateAsync(user, id, "Read", ctx);

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Risk-Adaptive Access Control.",
                decision = decision.Decision,
                riskScore = decision.RiskScore,
                operationalNeed = decision.OperationalNeedScore,
                reason = decision.Reason,
                factors = decision.FactorBreakdown
            });
        }

        return Ok(new
        {
            message = decision.Decision == "AllowWithConstraints"
                ? "Access granted with constraints (elevated risk, high operational need)."
                : "Access granted (acceptable risk).",
            decision = decision.Decision,
            riskScore = decision.RiskScore,
            reason = decision.Reason,
            content = $"RAdAC-protected content of '{resource.Name}'."
        });
    }

    /// <summary>
    /// Recent risk assessment logs (Admin).
    /// </summary>
    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLogs([FromQuery] int take = 50)
    {
        var logs = await _db.RiskAssessmentLogs.AsNoTracking()
            .OrderByDescending(l => l.AssessedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
        return Ok(logs);
    }
}

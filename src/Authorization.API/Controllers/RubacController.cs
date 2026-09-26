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
/// Rule-Based Access Control (RuBAC).
/// Predefined if-then rules (IP, time, department, role, rate-limit).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RubacController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRubacService _rubacService;

    public RubacController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IRubacService rubacService)
    {
        _db = db;
        _userManager = userManager;
        _rubacService = rubacService;
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

    [HttpPost("rules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRule([FromBody] CreateAccessRuleRequest request)
    {
        var rule = new AccessRule
        {
            Name = request.Name,
            Description = request.Description,
            Priority = request.Priority,
            Effect = request.Effect,
            ResourceType = request.ResourceType,
            Action = request.Action,
            SourceIpAllowList = request.SourceIpAllowList,
            SourceIpDenyList = request.SourceIpDenyList,
            TimeStartHour = request.TimeStartHour,
            TimeEndHour = request.TimeEndHour,
            DaysOfWeek = request.DaysOfWeek,
            RequiredDepartment = request.RequiredDepartment,
            RequiredRole = request.RequiredRole,
            RateLimitPerHour = request.RateLimitPerHour
        };
        _db.AccessRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(rule);
    }

    [HttpGet("rules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRules()
    {
        return Ok(await _rubacService.GetRulesAsync());
    }

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await _db.AccessRules.FindAsync(id);
        if (rule == null) return NotFound();
        rule.IsEnabled = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rule disabled." });
    }

    /// <summary>
    /// Evaluate current request against RuBAC rules.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] RubacEvaluateRequest request)
    {
        var (user, roles) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var ctx = new RuleEvaluationContext
        {
            UserId = user.Id,
            Roles = roles,
            Department = user.Department,
            ResourceType = request.ResourceType,
            Action = request.Action,
            SourceIp = request.SourceIp ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var decision = await _rubacService.EvaluateAsync(ctx);

        return Ok(new
        {
            allowed = decision.Allowed,
            reason = decision.Reason,
            matchedRule = decision.MatchedRuleName,
            priority = decision.MatchedPriority,
            model = "RuBAC"
        });
    }

    /// <summary>
    /// Access resource content under RuBAC rules.
    /// </summary>
    [HttpGet("resources/{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, [FromQuery] string? sourceIp = null)
    {
        var (user, roles) = await GetCurrentAsync();
        if (user == null) return Unauthorized();

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (resource == null) return NotFound();

        var ctx = new RuleEvaluationContext
        {
            UserId = user.Id,
            Roles = roles,
            Department = user.Department,
            ResourceType = resource.ResourceType,
            Action = "Read",
            SourceIp = sourceIp ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var decision = await _rubacService.EvaluateAsync(ctx);

        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Rule-Based Access Control.",
                reason = decision.Reason,
                matchedRule = decision.MatchedRuleName
            });
        }

        return Ok(new
        {
            message = "Access granted by RuBAC.",
            reason = decision.Reason,
            matchedRule = decision.MatchedRuleName,
            content = $"RuBAC-protected content of '{resource.Name}'."
        });
    }
}

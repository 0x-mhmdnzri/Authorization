using Authorization.API.DTOs;
using Authorization.API.Models;
using Authorization.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.API.Controllers;

/// <summary>
/// Mandatory Access Control (MAC) endpoints.
/// Access decisions are enforced by the system based on security labels.
/// Users / resource owners cannot override the policy (unlike DAC).
/// Implements Bell-LaPadula confidentiality model.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MacController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMacService _macService;

    public MacController(UserManager<ApplicationUser> userManager, IMacService macService)
    {
        _userManager = userManager;
        _macService = macService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Evaluate MAC decision for Read or Write on a resource.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateAccessRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        MacDecision decision = request.Action.Equals("Write", StringComparison.OrdinalIgnoreCase)
            ? await _macService.CanWriteAsync(user, request.ResourceId)
            : await _macService.CanReadAsync(user, request.ResourceId);

        return Ok(new
        {
            allowed = decision.Allowed,
            operation = decision.Operation,
            reason = decision.Reason,
            subjectClearance = decision.SubjectClearance,
            resourceClassification = decision.ResourceClassification,
            model = "MAC (Bell-LaPadula)"
        });
    }

    /// <summary>
    /// Attempt to read a resource under MAC rules.
    /// Returns 403 if clearance is insufficient (no read up).
    /// </summary>
    [HttpGet("resources/{id:guid}/read")]
    public async Task<IActionResult> ReadResource(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var decision = await _macService.CanReadAsync(user, id);
        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Access denied by Mandatory Access Control.",
                reason = decision.Reason,
                subjectClearance = decision.SubjectClearance,
                resourceClassification = decision.ResourceClassification
            });
        }

        var resource = await _macService.GetResourceAsync(id);
        return Ok(new
        {
            message = "MAC Read allowed.",
            reason = decision.Reason,
            resource = new
            {
                resource!.Id,
                resource.Name,
                resource.ResourceType,
                classification = resource.Sensitivity,
                resource.OwnerDepartment
            },
            content = $"[CLASSIFIED {resource.Sensitivity}] Content of '{resource.Name}'."
        });
    }

    /// <summary>
    /// Attempt to write/update a resource under MAC rules.
    /// Returns 403 on write-down violation.
    /// </summary>
    [HttpPost("resources/{id:guid}/write")]
    public async Task<IActionResult> WriteResource(Guid id, [FromBody] object? payload = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var decision = await _macService.CanWriteAsync(user, id);
        if (!decision.Allowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Write denied by Mandatory Access Control (possible write-down violation).",
                reason = decision.Reason,
                subjectClearance = decision.SubjectClearance,
                resourceClassification = decision.ResourceClassification
            });
        }

        var resource = await _macService.GetResourceAsync(id);
        return Ok(new
        {
            message = "MAC Write allowed (simulation – no actual mutation in demo).",
            reason = decision.Reason,
            resourceId = id,
            resourceName = resource?.Name,
            note = "In a real system the write would be performed here under the same clearance constraints."
        });
    }

    /// <summary>
    /// List available security levels (for reference / UI).
    /// </summary>
    [HttpGet("levels")]
    [AllowAnonymous]
    public IActionResult GetSecurityLevels()
    {
        var levels = SecurityLevel.AllLevels
            .Select(l => new { level = l, rank = SecurityLevel.GetRank(l) });
        return Ok(new
        {
            description = "MAC security levels ordered by rank (higher = more sensitive).",
            levels
        });
    }

    /// <summary>
    /// Show current user's clearance (from Identity).
    /// </summary>
    [HttpGet("my-clearance")]
    public async Task<IActionResult> MyClearance()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            clearance = user.ClearanceLevel ?? SecurityLevel.Public,
            rank = SecurityLevel.GetRank(user.ClearanceLevel),
            department = user.Department
        });
    }
}

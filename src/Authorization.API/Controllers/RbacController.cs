using Authorization.API.DTOs;
using Authorization.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Controllers;

/// <summary>
/// RBAC (Role-Based Access Control) endpoints.
/// Demonstrates classic RBAC using ASP.NET Core Identity roles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class RbacController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RbacController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// List all roles (Admin only).
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new { r.Id, r.Name, r.Description, r.CreatedAt })
            .ToListAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Create a new role (Admin only).
    /// </summary>
    [HttpPost("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
            return BadRequest(new { message = $"Role '{request.Name}' already exists." });

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, new { role.Id, role.Name, role.Description });
    }

    /// <summary>
    /// Assign a role to a user (Admin only).
    /// </summary>
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
            return BadRequest(new { message = $"Role '{request.RoleName}' does not exist." });

        if (await _userManager.IsInRoleAsync(user, request.RoleName))
            return BadRequest(new { message = "User already has this role." });

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.RoleName}' assigned to user {user.Email}." });
    }

    /// <summary>
    /// Remove a role from a user (Admin only).
    /// </summary>
    [HttpDelete("remove-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!await _userManager.IsInRoleAsync(user, request.RoleName))
            return BadRequest(new { message = "User does not have this role." });

        var result = await _userManager.RemoveFromRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.RoleName}' removed from user {user.Email}." });
    }

    /// <summary>
    /// Get roles of a specific user.
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { UserId = userId, Email = user.Email, Roles = roles });
    }

    /// <summary>
    /// Example protected endpoint that requires "Admin" role (classic RBAC).
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You have Admin role – RBAC check passed." });
    }

    /// <summary>
    /// Example protected endpoint that requires "Manager" or "Admin" role.
    /// </summary>
    [HttpGet("manager-area")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult ManagerArea()
    {
        return Ok(new { message = "You have Manager or Admin role – RBAC check passed." });
    }

    /// <summary>
    /// Endpoint accessible by any authenticated user (role "User" or higher).
    /// </summary>
    [HttpGet("user-area")]
    [Authorize] // any authenticated user
    public IActionResult UserArea()
    {
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new { message = "Authenticated user area.", YourRoles = roles });
    }
}

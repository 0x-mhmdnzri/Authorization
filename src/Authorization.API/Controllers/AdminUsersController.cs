using Authorization.API.Data;
using Authorization.API.DTOs;
using Authorization.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Controllers;

/// <summary>
/// User &amp; section-permission administration.
/// Methods used: RBAC (Admin/Manager roles) + DAC (section grants) + GOD bypass.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    private async Task<ApplicationUser?> CurrentUserAsync()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        if (id is null) return null;
        return await _userManager.FindByIdAsync(id);
    }

    private static bool CanManageUsers(ApplicationUser actor, IList<string> roles) =>
        actor.IsGod || roles.Contains("Admin") || roles.Contains("Manager");

    [HttpGet]
    public async Task<IActionResult> ListUsers()
    {
        var actor = await CurrentUserAsync();
        if (actor is null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(actor);
        if (!CanManageUsers(actor, roles))
            return Forbid();

        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Department,
                u.ClearanceLevel,
                u.IsActive,
                u.IsGod,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>Create user. Only GOD or Admin. Cannot create another GOD.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var actor = await CurrentUserAsync();
        if (actor is null) return Unauthorized();
        var actorRoles = await _userManager.GetRolesAsync(actor);
        if (!actor.IsGod && !actorRoles.Contains("Admin"))
            return Forbid();

        if (await _userManager.FindByEmailAsync(request.Email) != null)
            return BadRequest(new { message = "User already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            ClearanceLevel = request.ClearanceLevel,
            EmailConfirmed = true,
            IsActive = true,
            IsGod = false // never create GOD via API
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var rolesToAssign = request.Roles?.Count > 0 ? request.Roles : new List<string> { "User" };
        foreach (var roleName in rolesToAssign)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                await _userManager.AddToRoleAsync(user, roleName);
        }

        return CreatedAtAction(nameof(ListUsers), new { id = user.Id }, new
        {
            user.Id,
            user.Email,
            Roles = rolesToAssign
        });
    }

    /// <summary>
    /// Assign read/write on a menu section to a user.
    /// Method: DAC (granter decides) constrained by RBAC (only GOD/Admin/Manager).
    /// </summary>
    [HttpPost("section-permissions")]
    public async Task<IActionResult> AssignSectionPermission([FromBody] AssignSectionPermissionRequest request)
    {
        var actor = await CurrentUserAsync();
        if (actor is null) return Unauthorized();
        var actorRoles = await _userManager.GetRolesAsync(actor);
        if (!CanManageUsers(actor, actorRoles))
            return Forbid();

        var target = await _userManager.FindByIdAsync(request.UserId);
        if (target is null)
            return NotFound(new { message = "User not found." });

        if (target.IsGod)
            return BadRequest(new { message = "Cannot assign section permissions to GOD (has implicit full access)." });

        var section = await _db.MenuSections.FirstOrDefaultAsync(s => s.Key == request.SectionKey && s.IsEnabled);
        if (section is null)
            return NotFound(new { message = $"Section '{request.SectionKey}' not found." });

        // Manager cannot grant write on sections they themselves cannot write (unless GOD/Admin)
        if (!actor.IsGod && !actorRoles.Contains("Admin"))
        {
            var actorGrant = await _db.SectionPermissions
                .Include(p => p.MenuSection)
                .Where(p => p.UserId == actor.Id && p.MenuSection!.Key == request.SectionKey && p.IsActive)
                .ToListAsync();

            if (request.CanWrite && !actorGrant.Any(g => g.CanWrite))
                return Forbid();
            if (request.CanRead && !actorGrant.Any(g => g.CanRead || g.CanWrite))
                return Forbid();
        }

        var existing = await _db.SectionPermissions
            .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.MenuSectionId == section.Id);

        if (existing is null)
        {
            _db.SectionPermissions.Add(new SectionPermission
            {
                MenuSectionId = section.Id,
                UserId = request.UserId,
                CanRead = request.CanRead,
                CanWrite = request.CanWrite,
                GrantedById = actor.Id,
                ExpiresAt = request.ExpiresAt
            });
        }
        else
        {
            existing.CanRead = request.CanRead;
            existing.CanWrite = request.CanWrite;
            existing.GrantedById = actor.Id;
            existing.GrantedAt = DateTime.UtcNow;
            existing.ExpiresAt = request.ExpiresAt;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Section permission assigned.", request.SectionKey, request.CanRead, request.CanWrite });
    }

    [HttpGet("{userId}/section-permissions")]
    public async Task<IActionResult> GetUserSectionPermissions(string userId)
    {
        var actor = await CurrentUserAsync();
        if (actor is null) return Unauthorized();
        var actorRoles = await _userManager.GetRolesAsync(actor);
        if (!CanManageUsers(actor, actorRoles) && actor.Id != userId)
            return Forbid();

        var list = await _db.SectionPermissions
            .Include(p => p.MenuSection)
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.Id,
                SectionKey = p.MenuSection!.Key,
                SectionTitle = p.MenuSection.Title,
                p.CanRead,
                p.CanWrite,
                p.GrantedAt,
                p.ExpiresAt,
                p.GrantedById
            })
            .ToListAsync();

        return Ok(list);
    }
}

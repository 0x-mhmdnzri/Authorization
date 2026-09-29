using Authorization.API.Data;
using Authorization.API.DTOs;
using Authorization.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Controllers;

/// <summary>
/// SSR-ready menu endpoint. Menu is fully driven from DB + section permissions.
/// GOD sees every enabled section with full read/write.
/// Method: RBAC (roles) + DAC (section grants) hybrid.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public MenuController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Returns hierarchical menu items the current user can at least read.
    /// Designed to be called from a Next.js Server Component.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MenuItemDto>>> GetMenu()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
            return Unauthorized();

        var sections = await _db.MenuSections
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        Dictionary<string, (bool read, bool write)> perms;

        if (user.IsGod)
        {
            perms = sections.ToDictionary(s => s.Key, _ => (true, true));
        }
        else
        {
            var grants = await _db.SectionPermissions
                .Include(p => p.MenuSection)
                .Where(p => p.UserId == userId && p.MenuSection != null)
                .ToListAsync();

            perms = grants
                .Where(g => g.IsActive && g.MenuSection != null)
                .GroupBy(g => g.MenuSection!.Key)
                .ToDictionary(
                    g => g.Key,
                    g => (g.Any(x => x.CanRead), g.Any(x => x.CanWrite)));

            // Admins get at least read on all sections if no explicit grant
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                foreach (var s in sections)
                {
                    if (!perms.ContainsKey(s.Key))
                        perms[s.Key] = (true, true);
                    else
                    {
                        var cur = perms[s.Key];
                        perms[s.Key] = (true, cur.write || true);
                    }
                }
            }
        }

        var visible = sections
            .Where(s => perms.TryGetValue(s.Key, out var p) && p.read)
            .Select(s =>
            {
                var p = perms[s.Key];
                return new MenuItemDto
                {
                    Key = s.Key,
                    Title = s.Title,
                    Description = s.Description,
                    Href = s.Href,
                    Icon = s.Icon,
                    AuthorizationMethods = s.AuthorizationMethods,
                    SortOrder = s.SortOrder,
                    CanRead = p.read,
                    CanWrite = p.write,
                    // parent linking done below
                };
            })
            .ToList();

        // Build hierarchy by ParentKey stored on entity
        var byKey = visible.ToDictionary(v => v.Key);
        var roots = new List<MenuItemDto>();

        foreach (var section in sections.Where(s => perms.TryGetValue(s.Key, out var p) && p.read))
        {
            if (!byKey.TryGetValue(section.Key, out var item))
                continue;

            if (!string.IsNullOrEmpty(section.ParentKey) && byKey.TryGetValue(section.ParentKey, out var parent))
                parent.Children.Add(item);
            else
                roots.Add(item);
        }

        return Ok(roots.OrderBy(r => r.SortOrder).ToList());
    }
}

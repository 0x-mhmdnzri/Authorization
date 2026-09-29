using Authorization.API.DTOs;
using Authorization.API.Models;
using Authorization.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "User already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            ClearanceLevel = request.ClearanceLevel,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new ApplicationRole { Name = "User", Description = "Default user role" });

        await _userManager.AddToRoleAsync(user, "User");

        var pair = await _tokenService.GenerateTokenPairAsync(user, ClientIp);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(ToAuthResponse(pair, user, roles));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var pair = await _tokenService.GenerateTokenPairAsync(user, ClientIp);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(ToAuthResponse(pair, user, roles));
    }

    /// <summary>Rotate refresh token and issue new access + refresh (A3).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var pair = await _tokenService.RefreshAsync(request.RefreshToken, ClientIp);
        if (pair is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(new
        {
            pair.AccessToken,
            pair.AccessTokenExpiration,
            pair.RefreshToken,
            pair.RefreshTokenExpiration,
            Token = pair.AccessToken,
            Expiration = pair.AccessTokenExpiration
        });
    }

    /// <summary>Extend refresh lifetime and issue new access token without rotation (A3).</summary>
    [HttpPost("renew")]
    [AllowAnonymous]
    public async Task<IActionResult> Renew([FromBody] RefreshRequest request)
    {
        var pair = await _tokenService.RenewAsync(request.RefreshToken, ClientIp);
        if (pair is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(new
        {
            pair.AccessToken,
            pair.AccessTokenExpiration,
            pair.RefreshToken,
            pair.RefreshTokenExpiration,
            Token = pair.AccessToken,
            Expiration = pair.AccessTokenExpiration
        });
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        var ok = await _tokenService.RevokeAsync(request.RefreshToken, ClientIp, "User logout");
        if (!ok)
            return BadRequest(new { message = "Token not found or already revoked." });
        return Ok(new { message = "Revoked." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Department,
            user.ClearanceLevel,
            user.IsGod,
            Roles = roles
        });
    }

    private static AuthResponse ToAuthResponse(TokenPair pair, ApplicationUser user, IList<string> roles) => new()
    {
        AccessToken = pair.AccessToken,
        AccessTokenExpiration = pair.AccessTokenExpiration,
        RefreshToken = pair.RefreshToken,
        RefreshTokenExpiration = pair.RefreshTokenExpiration,
        UserId = user.Id,
        Email = user.Email!,
        Roles = roles,
        IsGod = user.IsGod
    };
}

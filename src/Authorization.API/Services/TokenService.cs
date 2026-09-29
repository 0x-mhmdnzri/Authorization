using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authorization.API.Data;
using Authorization.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Authorization.API.Services;

public interface ITokenService
{
    Task<TokenPair> GenerateTokenPairAsync(ApplicationUser user, string? ipAddress = null);
    Task<TokenPair?> RefreshAsync(string refreshToken, string? ipAddress = null);
    Task<TokenPair?> RenewAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeAsync(string refreshToken, string? ipAddress = null, string? reason = null);
    DateTime GetAccessExpiration();
}

public record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiration,
    string RefreshToken,
    DateTime RefreshTokenExpiration);

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    private readonly TimeSpan _accessLifetime = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _refreshLifetime = TimeSpan.FromDays(7);

    public TokenService(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _configuration = configuration;
        _userManager = userManager;
        _db = db;
    }

    public DateTime GetAccessExpiration() => DateTime.UtcNow.Add(_accessLifetime);

    public async Task<TokenPair> GenerateTokenPairAsync(ApplicationUser user, string? ipAddress = null)
    {
        var accessToken = await GenerateAccessTokenAsync(user);
        var refresh = await CreateRefreshTokenAsync(user.Id, ipAddress);
        return new TokenPair(accessToken, GetAccessExpiration(), refresh.Token, refresh.ExpiresAt);
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, string? ipAddress = null)
    {
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (existing is null || !existing.IsActive || existing.User is null || !existing.User.IsActive)
            return null;

        // Rotate: revoke old, issue new pair
        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReasonRevoked = "Replaced by refresh";

        var newPair = await GenerateTokenPairAsync(existing.User, ipAddress);
        existing.ReplacedByToken = newPair.RefreshToken;
        await _db.SaveChangesAsync();

        return newPair;
    }

    /// <summary>
    /// Renew extends the same refresh token lifetime and issues a new access token
    /// without rotating the refresh token (useful for sliding sessions).
    /// </summary>
    public async Task<TokenPair?> RenewAsync(string refreshToken, string? ipAddress = null)
    {
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (existing is null || !existing.IsActive || existing.User is null || !existing.User.IsActive)
            return null;

        existing.ExpiresAt = DateTime.UtcNow.Add(_refreshLifetime);
        await _db.SaveChangesAsync();

        var accessToken = await GenerateAccessTokenAsync(existing.User);
        return new TokenPair(accessToken, GetAccessExpiration(), existing.Token, existing.ExpiresAt);
    }

    public async Task<bool> RevokeAsync(string refreshToken, string? ipAddress = null, string? reason = null)
    {
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (existing is null || existing.IsRevoked)
            return false;

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReasonRevoked = reason ?? "Revoked by user";
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName ?? string.Empty),
            new("lastName", user.LastName ?? string.Empty),
            new("department", user.Department ?? string.Empty),
            new("clearance", user.ClearanceLevel ?? string.Empty),
            new("is_god", user.IsGod ? "true" : "false")
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(_accessLifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(string userId, string? ipAddress)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.Add(_refreshLifetime),
            CreatedByIp = ipAddress
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }
}

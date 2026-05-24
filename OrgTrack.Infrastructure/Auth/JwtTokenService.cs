using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OrgTrack.Application.Interfaces;

namespace OrgTrack.Infrastructure.Auth;

public class JwtTokenService(string secret) : ITokenService
{
    private const int AccessTokenExpirationHours = 1;
    private const int RefreshTokenSizeBytes = 64; // 128 caractere hex

    public string GenerateAccessToken(Guid userId, string email)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID unic per token
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(AccessTokenExpirationHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes);
        return Convert.ToHexString(bytes); // 128 caractere hex, URL-safe
    }
}
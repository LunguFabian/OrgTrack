using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Models;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class DevAuthController(
    IUserRepository userRepository,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// DEV ONLY!
    /// Allows testing without Google — creates/finds a user and returns a JWT.
    /// This endpoint will NOT exist in production.
    /// </summary>
    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request)
    {
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName ?? "Test",
                LastName = request.LastName ?? "User",
                IsActive = true
            };
            await userRepository.AddAsync(user);
        }

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await refreshTokenRepository.AddAsync(refreshTokenEntity);
        
        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PictureUrl, user.IsGoogleCalendarConnected)
        ));
    }
}

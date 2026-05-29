using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Models;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AuthenticateUser authenticateUser,
    IUserRepository userRepository,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Google login (for production).
    /// The frontend sends the token received from Google.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await authenticateUser.ExecuteAsync(request.IdToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new AuthResponse(
            result.AccessToken!,
            result.RefreshToken!,
            new UserDto(result.UserId!.Value, result.Email!, result.FirstName!, result.LastName!, result.PictureUrl, result.IsGoogleCalendarConnected)
        ));
    }

    /// <summary>
    /// Refresh token endpoint.
    /// Receives the refresh token and returns a new Access Token and a new Refresh Token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authenticateUser.RefreshAsync(request.RefreshToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new AuthResponse(
            result.AccessToken!,
            result.RefreshToken!,
            new UserDto(result.UserId!.Value, result.Email!, result.FirstName!, result.LastName!, result.PictureUrl, result.IsGoogleCalendarConnected)
        ));
    }

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

    /// <summary>
    /// Receives the authorization code from the frontend, exchanges it for tokens, and connects the user's Google Calendar.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("google-calendar")]
    public async Task<IActionResult> ConnectGoogleCalendar(
        [FromBody] GoogleCalendarConnectRequest request,
        [FromServices] ConnectGoogleCalendar connectUseCase)
    {
        var userId = OrgTrack.Api.Extensions.ClaimsPrincipalExtensions.GetUserId(User);
        
        var success = await connectUseCase.ExecuteAsync(userId, request.AuthorizationCode, request.RedirectUri);
        
        if (!success)
        {
            return BadRequest(new { error = "Failed to connect Google Calendar." });
        }

        return Ok(new { connected = true });
    }
}

public record GoogleCalendarConnectRequest(string AuthorizationCode, string RedirectUri);
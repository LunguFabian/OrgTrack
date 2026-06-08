using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

public class AuthenticateUser(
    IUserRepository userRepository,
    IGoogleAuthService googleAuthService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    OrgTrack.Domain.Interfaces.IEmailService emailService)
{
    private const int RefreshTokenExpirationDays = 30;

    /// <summary>
    /// The complete Google login flow:
    /// 1. Validates the Google token
    /// 2. Finds or creates the user in the DB
    /// 3. Updates the profile (if anything changed on Google)
    /// 4. Generates Access Token (JWT, 1 hour) + Refresh Token (random, 30 days)
    /// 5. Saves the Refresh Token in the DB
    /// 6. Returns both tokens
    /// </summary>
    public async Task<AuthResult> ExecuteAsync(string googleIdToken)
    {
        var googleUser = await googleAuthService.ValidateGoogleTokenAsync(googleIdToken);
        if (googleUser == null)
            return AuthResult.Failed("Token Google invalid sau expirat.");

        var user = await userRepository.GetByEmailAsync(googleUser.Email);

        if (user == null)
        {
            user = new User
            {
                Email = googleUser.Email,
                FirstName = googleUser.FirstName,
                LastName = googleUser.LastName,
                PictureUrl = googleUser.PictureUrl,
                IsActive = true
            };
            await userRepository.AddAsync(user);

            var baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
            // Send welcome email asynchronously without blocking the login
            _ = emailService.SendEmailAsync(
                user.Email,
                "Welcome to OrgTrack! 🚀",
                OrgTrack.Application.Helpers.EmailTemplates.GetWelcomeEmail(user.FirstName, baseUrl.TrimEnd('/'))
            );
        }
        else
        {
            user.FirstName = googleUser.FirstName;
            user.LastName = googleUser.LastName;
            user.PictureUrl = googleUser.PictureUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);
        }

        return await IssueTokensAsync(user);
    }

    /// <summary>
    /// Refresh Token Flow (with Rotation + Theft Detection):
    /// 1. Find the refresh token in the DB
    /// 2. If it does not exist → 401
    /// 3. If it exists but is REVOKED → THEFT DETECTION → revoke ALL user tokens
    /// 4. If it is expired → 401
    /// 5. Revoke the current token (rotation)
    /// 6. Issue new Access Token + Refresh Token
    /// </summary>
    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken == null)
            return AuthResult.Failed("Refresh token invalid.");
        if (storedToken.IsRevoked)
        {
            await refreshTokenRepository.RevokeAllForUserAsync(
                storedToken.UserId, "TheftDetected - Revoked token reuse attempt");
            return AuthResult.Failed("Token de securitate compromis. Te rugăm să te autentifici din nou.");
        }
        if (storedToken.IsExpired)
            return AuthResult.Failed("Sesiunea a expirat. Te rugăm să te autentifici din nou.");
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedReason = "Used";
        await refreshTokenRepository.UpdateAsync(storedToken);
        var user = storedToken.User!;
        return await IssueTokensAsync(user);
    }
    private async Task<AuthResult> IssueTokensAsync(User user)
    {
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays)
        };

        await refreshTokenRepository.AddAsync(refreshTokenEntity);

        return AuthResult.Success(accessToken, refreshTokenValue, user);
    }
}
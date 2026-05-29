using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

public class ConnectGoogleCalendar(
    IUserRepository userRepository,
    IGoogleAuthService googleAuthService)
{
    public async Task<bool> ExecuteAsync(Guid userId, string authorizationCode, string redirectUri)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        var tokens = await googleAuthService.ExchangeAuthCodeForTokensAsync(authorizationCode, redirectUri);
        if (tokens == null) return false;

        user.GoogleCalendarAccessToken = tokens.AccessToken;
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            user.GoogleCalendarRefreshToken = tokens.RefreshToken;
        }
        user.IsGoogleCalendarConnected = true;

        await userRepository.UpdateAsync(user);

        return true;
    }
}

public record GoogleTokenResponse(string AccessToken, string? RefreshToken);

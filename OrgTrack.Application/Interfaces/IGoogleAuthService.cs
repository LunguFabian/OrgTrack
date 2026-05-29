namespace OrgTrack.Application.Interfaces;
public record GoogleUserInfo(string Email, string FirstName, string LastName, string? PictureUrl);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string googleIdToken);
    Task<OrgTrack.Application.UseCases.GoogleTokenResponse?> ExchangeAuthCodeForTokensAsync(string authorizationCode, string redirectUri);
}

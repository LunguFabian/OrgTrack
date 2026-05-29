using Google.Apis.Auth;
using OrgTrack.Application.Interfaces;

namespace OrgTrack.Infrastructure.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GoogleAuthService(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string googleIdToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, settings);
            return new GoogleUserInfo(
                Email: payload.Email,
                FirstName: payload.GivenName ?? string.Empty,
                LastName: payload.FamilyName ?? string.Empty,
                PictureUrl: payload.Picture
            );
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }

    public async Task<OrgTrack.Application.UseCases.GoogleTokenResponse?> ExchangeAuthCodeForTokensAsync(string authorizationCode, string redirectUri)
    {
        try
        {
            var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = new[] { Google.Apis.Calendar.v3.CalendarService.Scope.CalendarEvents }
            });

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId: "user",
                code: authorizationCode,
                redirectUri: redirectUri,
                taskCancellationToken: CancellationToken.None
            );

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return new OrgTrack.Application.UseCases.GoogleTokenResponse(
                    tokenResponse.AccessToken,
                    tokenResponse.RefreshToken
                );
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exchanging auth code: {ex.Message}");
            return null;
        }
    }
}

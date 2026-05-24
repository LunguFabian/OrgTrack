using Google.Apis.Auth;
using OrgTrack.Application.Interfaces;

namespace OrgTrack.Infrastructure.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;

    public GoogleAuthService(string clientId)
    {
        _clientId = clientId;
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
}

using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.DTOs;

public record AuthResult(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAt,
    Guid? UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PictureUrl,
    bool IsGoogleCalendarConnected,
    string? ErrorMessage)
{
    public static AuthResult Success(string accessToken, string refreshToken, User user)
        => new(
            IsSuccess: true,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddHours(1),
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PictureUrl: user.PictureUrl,
            IsGoogleCalendarConnected: user.IsGoogleCalendarConnected,
            ErrorMessage: null);

    public static AuthResult Failed(string error)
        => new(false, null, null, null, null, null, null, null, null, false, error);
}

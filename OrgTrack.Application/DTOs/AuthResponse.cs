namespace OrgTrack.Application.DTOs;

public record AuthResponse(string Token, string RefreshToken, UserDto User);

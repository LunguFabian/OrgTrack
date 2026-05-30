using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class ConnectGoogleCalendarTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ConnectGoogleCalendar _connectGoogleCalendar;

    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;

    public ConnectGoogleCalendarTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _googleAuthServiceMock = new Mock<IGoogleAuthService>();
        _connectGoogleCalendar = new ConnectGoogleCalendar(_userRepositoryMock.Object, _googleAuthServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _connectGoogleCalendar.ExecuteAsync(userId, "auth_code", "redirect_uri");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateUserTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _googleAuthServiceMock.Setup(r => r.ExchangeAuthCodeForTokensAsync("auth_code", "redirect_uri"))
            .ReturnsAsync(new GoogleTokenResponse("access_token", "refresh_token"));
            
        _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await _connectGoogleCalendar.ExecuteAsync(userId, "auth_code", "redirect_uri");

        result.Should().BeTrue();
        user.GoogleCalendarAccessToken.Should().Be("access_token");
        user.GoogleCalendarRefreshToken.Should().Be("refresh_token");
        user.IsGoogleCalendarConnected.Should().BeTrue();
        
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }
}

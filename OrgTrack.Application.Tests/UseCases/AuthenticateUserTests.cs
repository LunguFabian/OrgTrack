using FluentAssertions;
using Moq;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests.UseCases;

public class AuthenticateUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly AuthenticateUser _sut;

    public AuthenticateUserTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _googleAuthServiceMock = new Mock<IGoogleAuthService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _sut = new AuthenticateUser(
            _userRepositoryMock.Object,
            _googleAuthServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailed_WhenGoogleTokenIsInvalid()
    {
        // Arrange
        _googleAuthServiceMock
            .Setup(x => x.ValidateGoogleTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((GoogleUserInfo?)null);

        // Act
        var result = await _sut.ExecuteAsync("invalid_token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Token Google invalid sau expirat.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateUser_WhenUserDoesNotExist()
    {
        // Arrange
        var googleUser = new GoogleUserInfo("new@user.com", "New", "User", null);
        _googleAuthServiceMock.Setup(x => x.ValidateGoogleTokenAsync("valid_token")).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(googleUser.Email)).ReturnsAsync((User?)null);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), googleUser.Email)).Returns("access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

        // Act
        var result = await _sut.ExecuteAsync("valid_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == googleUser.Email)), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnFailed_WhenTokenDoesNotExist()
    {
        // Arrange
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("invalid_token")).ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.RefreshAsync("invalid_token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Refresh token invalid.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeAllTokens_WhenTokenIsAlreadyRevoked_TheftDetection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var storedToken = new RefreshToken { Token = "stolen_token", IsRevoked = true, UserId = userId };
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("stolen_token")).ReturnsAsync(storedToken);

        // Act
        var result = await _sut.RefreshAsync("stolen_token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Token de securitate compromis");
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAllForUserAsync(userId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldIssueNewTokens_WhenTokenIsValid()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com" };
        var storedToken = new RefreshToken { Token = "valid_token", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1), User = user };
        
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("valid_token")).ReturnsAsync(storedToken);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user.Id, user.Email)).Returns("new_access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new_refresh_token");

        // Act
        var result = await _sut.RefreshAsync("valid_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        
        // Ensure old token was revoked
        storedToken.IsRevoked.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(x => x.UpdateAsync(storedToken), Times.Once);
        
        // Ensure new token was added
        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt => rt.Token == "new_refresh_token")), Times.Once);
    }
}

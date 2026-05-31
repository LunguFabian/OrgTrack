using FluentAssertions;
using Moq;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class AuthenticateUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly AuthenticateUser _authenticateUser;

    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<OrgTrack.Domain.Interfaces.IEmailService> _emailServiceMock;

    public AuthenticateUserTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _googleAuthServiceMock = new Mock<IGoogleAuthService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _emailServiceMock = new Mock<OrgTrack.Domain.Interfaces.IEmailService>();
        
        _authenticateUser = new AuthenticateUser(
            _userRepositoryMock.Object,
            _googleAuthServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _emailServiceMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateUser_WhenUserDoesNotExist()
    {
        var email = "test@aiesec.ro";
        var googleToken = "valid_token";
        
        var googleUser = new GoogleUserInfo(email, "Test", "User", "http://example.com/pic.png");

        _googleAuthServiceMock.Setup(r => r.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User?)null);
        
        User? savedUser = null;
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.Id = Guid.NewGuid(); savedUser = u; })
            .Returns(Task.CompletedTask);

        _tokenServiceMock.Setup(r => r.GenerateAccessToken(It.IsAny<Guid>(), email)).Returns("access");
        _tokenServiceMock.Setup(r => r.GenerateRefreshToken()).Returns("refresh");

        var result = await _authenticateUser.ExecuteAsync(googleToken);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
        
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(email);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateUser_WhenUserExists()
    {
        var email = "test@aiesec.ro";
        var googleToken = "valid_token";
        var user = new User { Id = Guid.NewGuid(), Email = email, FirstName = "Old", LastName = "Name", IsActive = false };
        
        var googleUser = new GoogleUserInfo(email, "New", "Name", "pic");

        _googleAuthServiceMock.Setup(r => r.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        
        _tokenServiceMock.Setup(r => r.GenerateAccessToken(user.Id, email)).Returns("access");
        _tokenServiceMock.Setup(r => r.GenerateRefreshToken()).Returns("refresh");

        var result = await _authenticateUser.ExecuteAsync(googleToken);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        // Assert that the user was updated
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name");
        user.PictureUrl.Should().Be("pic");
    }
}

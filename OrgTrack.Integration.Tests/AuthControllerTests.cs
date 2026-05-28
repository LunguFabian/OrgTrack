using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using OrgTrack.Api.Models;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Infrastructure.Persistence;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task DevLogin_ShouldReturnTokens_WhenCalledInTestEnvironment()
    {
        // Act
        var request = new DevLoginRequest("testdev@aiesec.net", "Test", "Dev");
        var response = await _client.PostAsJsonAsync("/api/auth/dev-login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
        content.User.Email.Should().Be("testdev@aiesec.net");
    }

    [Fact]
    public async Task Refresh_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "refresh@test.com", FirstName = "Refresh", LastName = "User" };
        var refreshToken = new RefreshToken
        {
            Token = "valid_refresh_token_for_test",
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrgTrackDbContext>();
            db.Users.Add(user);
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync();
        }

        var request = new RefreshRequest { RefreshToken = "valid_refresh_token_for_test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBe("valid_refresh_token_for_test");
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "invalid_refresh_token_for_test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

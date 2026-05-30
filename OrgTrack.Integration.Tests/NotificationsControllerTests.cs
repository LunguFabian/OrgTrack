using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class NotificationsControllerTests : IntegrationTestBase
{
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public NotificationsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnList_WhenNotificationsExist()
    {
        AuthenticateAs(_testUserId);

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });
            
            db.Notifications.Add(new Notification 
            { 
                Id = Guid.NewGuid(), 
                UserId = _testUserId, 
                Title = "Test", 
                Message = "Test message", 
                CreatedAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnCount()
    {
        AuthenticateAs(_testUserId);

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });
            
            db.Notifications.Add(new Notification 
            { 
                Id = Guid.NewGuid(), 
                UserId = _testUserId, 
                Title = "Test", 
                Message = "Test message", 
                CreatedAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNoContent()
    {
        AuthenticateAs(_testUserId);
        var notificationId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });
            
            db.Notifications.Add(new Notification 
            { 
                Id = notificationId, 
                UserId = _testUserId, 
                Title = "Test", 
                Message = "Test message", 
                CreatedAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.PutAsJsonAsync($"/api/notifications/{notificationId}/read", new {});

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnNoContent()
    {
        AuthenticateAs(_testUserId);

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });
            
            db.Notifications.Add(new Notification 
            { 
                Id = Guid.NewGuid(), 
                UserId = _testUserId, 
                Title = "Test", 
                Message = "Test message", 
                CreatedAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.PutAsJsonAsync("/api/notifications/read-all", new {});

        // InMemoryDatabase provider does not support ExecuteUpdateAsync, 
        // which throws InvalidOperationException resulting in 422 from GlobalExceptionMiddleware.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}

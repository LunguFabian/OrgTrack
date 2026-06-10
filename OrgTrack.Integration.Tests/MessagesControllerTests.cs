using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Api.Controllers;
using OrgTrack.Application.DTOs;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class MessagesControllerTests : IntegrationTestBase
{
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public MessagesControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SendMessage_ShouldReturnOk_AndCreateMessage()
    {
        AuthenticateAs(_testUserId);
        var targetUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Sender", LastName = "User", Email = "sender@test.com" });
            db.Users.Add(new User { Id = targetUserId, FirstName = "Receiver", LastName = "User", Email = "receiver@test.com" });
            await db.SaveChangesAsync();
        });

        var request = new SendMessageRequest(targetUserId, "Hello Integration Tests!");

        var response = await Client.PostAsJsonAsync("/api/messages/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messageDto = await response.Content.ReadFromJsonAsync<MessageDto>();
        messageDto.Should().NotBeNull();
        messageDto!.Content.Should().Be("Hello Integration Tests!");
    }

    [Fact]
    public async Task GetConversations_ShouldReturnList_WhenMessagesExist()
    {
        AuthenticateAs(_testUserId);
        var targetUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Sender", LastName = "User", Email = "sender@test.com" });
            db.Users.Add(new User { Id = targetUserId, FirstName = "Receiver", LastName = "User", Email = "receiver@test.com" });
            
            db.Messages.Add(new Message 
            { 
                Id = Guid.NewGuid(), 
                SenderId = targetUserId, 
                ReceiverId = _testUserId, 
                Content = "Hi back", 
                SentAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/api/messages/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversations = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        conversations.Should().NotBeNull();
        conversations.Should().HaveCount(1);
    }

    [Fact]
    public async Task MarkConversationAsRead_ShouldReturnNoContent()
    {
        AuthenticateAs(_testUserId);
        var targetUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Sender", LastName = "User", Email = "sender@test.com" });
            db.Users.Add(new User { Id = targetUserId, FirstName = "Receiver", LastName = "User", Email = "receiver@test.com" });
            
            db.Messages.Add(new Message 
            { 
                Id = Guid.NewGuid(), 
                SenderId = targetUserId, 
                ReceiverId = _testUserId, 
                Content = "Read me", 
                SentAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.PutAsJsonAsync($"/api/messages/conversations/{targetUserId}/read", new {});

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetConversationMessages_ShouldReturnMessages()
    {
        AuthenticateAs(_testUserId);
        var targetUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Sender", LastName = "User", Email = "sender@test.com" });
            db.Users.Add(new User { Id = targetUserId, FirstName = "Receiver", LastName = "User", Email = "receiver@test.com" });
            
            db.Messages.Add(new Message 
            { 
                Id = Guid.NewGuid(), 
                SenderId = targetUserId, 
                ReceiverId = _testUserId, 
                Content = "Read me", 
                SentAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync($"/api/messages/conversations/{targetUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnreadTotalCount_ShouldReturnCount()
    {
        AuthenticateAs(_testUserId);
        var targetUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Sender", LastName = "User", Email = "sender@test.com" });
            db.Users.Add(new User { Id = targetUserId, FirstName = "Receiver", LastName = "User", Email = "receiver@test.com" });
            
            db.Messages.Add(new Message 
            { 
                Id = Guid.NewGuid(), 
                SenderId = targetUserId, 
                ReceiverId = _testUserId, 
                Content = "Read me", 
                SentAt = DateTime.UtcNow, 
                IsRead = false 
            });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/api/messages/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenReceiverIdIsNull()
    {
        AuthenticateAs(_testUserId);
        var request = new SendMessageRequest(null, "Hello Integration Tests!");
        var response = await Client.PostAsJsonAsync("/api/messages/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenContentIsEmpty()
    {
        AuthenticateAs(_testUserId);
        var request = new SendMessageRequest(Guid.NewGuid(), "");
        var response = await Client.PostAsJsonAsync("/api/messages/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

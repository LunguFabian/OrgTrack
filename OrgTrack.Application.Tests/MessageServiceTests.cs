using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRealtimeNotifier> _realtimeNotifierMock;
    private readonly MessageService _messageService;

    public MessageServiceTests()
    {
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _realtimeNotifierMock = new Mock<IRealtimeNotifier>();

        _messageService = new MessageService(
            _messageRepositoryMock.Object,
            _userRepositoryMock.Object,
            _realtimeNotifierMock.Object
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowException_WhenReceiverDoesNotExist()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        
        _userRepositoryMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(new User { Id = senderId });
        _userRepositoryMock.Setup(r => r.GetByIdAsync(receiverId)).ReturnsAsync((User?)null);

        var action = async () => await _messageService.SendMessageAsync(senderId, receiverId, "Hello");

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Receiver not found");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCreateMessageAndNotify()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var receiver = new User { Id = receiverId, FirstName = "Jane" };
        var sender = new User { Id = senderId, FirstName = "John" };
        
        _userRepositoryMock.Setup(r => r.GetByIdAsync(receiverId)).ReturnsAsync(receiver);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(sender);
        
        Message? savedMessage = null;
        _messageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Message>()))
            .Callback<Message>(m => savedMessage = m)
            .ReturnsAsync((Message m) => m);

        var result = await _messageService.SendMessageAsync(senderId, receiverId, "Hello World");

        result.Should().NotBeNull();
        result.Content.Should().Be("Hello World");
        
        savedMessage.Should().NotBeNull();
        savedMessage!.Content.Should().Be("Hello World");
        savedMessage.SenderId.Should().Be(senderId);
        
        _realtimeNotifierMock.Verify(r => r.SendToUserAsync(receiverId, "ReceiveMessage", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetConversationAsync_ShouldReturnMessages()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var messages = new List<Message>
        {
            new Message { Id = Guid.NewGuid(), Content = "Hello", Sender = new User { Id = otherUserId, FirstName = "A", LastName = "B" } },
            new Message { Id = Guid.NewGuid(), Content = "Hi", Sender = new User { Id = currentUserId, FirstName = "C", LastName = "D" } }
        };

        _messageRepositoryMock.Setup(r => r.GetConversationAsync(currentUserId, otherUserId, 50, 0)).ReturnsAsync(messages);

        var result = await _messageService.GetConversationAsync(currentUserId, otherUserId);

        result.Should().HaveCount(2);
        // Reversed order in service logic
        result[0].Content.Should().Be("Hi"); 
        result[1].Content.Should().Be("Hello");
    }

    [Fact]
    public async Task GetConversationsListAsync_ShouldReturnConversationsList()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var recentMessages = new List<Message>
        {
            new Message 
            { 
                Id = Guid.NewGuid(), 
                Content = "Hey", 
                SenderId = currentUserId, 
                ReceiverId = otherUserId,
                Sender = new User { Id = currentUserId, FirstName = "Me", LastName = "Myself" },
                Receiver = new User { Id = otherUserId, FirstName = "Other", LastName = "Person" },
                SentAt = DateTime.UtcNow
            }
        };

        _messageRepositoryMock.Setup(r => r.GetRecentMessagesAsync(currentUserId)).ReturnsAsync(recentMessages);

        var result = await _messageService.GetConversationsListAsync(currentUserId);

        result.Should().HaveCount(1);
        result[0].OtherUserId.Should().Be(otherUserId);
        result[0].OtherUserName.Should().Be("Other Person");
        result[0].LastMessageContent.Should().Be("Hey");
        result[0].IsLastMessageSentByMe.Should().BeTrue();
    }

    [Fact]
    public async Task MarkConversationAsReadAsync_ShouldCallRepository()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _messageRepositoryMock.Setup(r => r.MarkAsReadAsync(otherUserId, currentUserId)).Returns(Task.CompletedTask);

        await _messageService.MarkConversationAsReadAsync(currentUserId, otherUserId);

        _messageRepositoryMock.Verify(r => r.MarkAsReadAsync(otherUserId, currentUserId), Times.Once);
    }

    [Fact]
    public async Task GetUnreadTotalCountAsync_ShouldReturnCount()
    {
        var userId = Guid.NewGuid();
        _messageRepositoryMock.Setup(r => r.GetUnreadCountAsync(userId)).ReturnsAsync(5);

        var result = await _messageService.GetUnreadTotalCountAsync(userId);

        result.Should().Be(5);
    }
}

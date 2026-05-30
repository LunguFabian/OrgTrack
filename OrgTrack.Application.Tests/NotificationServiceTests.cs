using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IRealtimeNotifier> _realtimeNotifierMock;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _realtimeNotifierMock = new Mock<IRealtimeNotifier>();

        _notificationService = new NotificationService(
            _notificationRepositoryMock.Object,
            _realtimeNotifierMock.Object
        );
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldCreateAndNotify()
    {
        var userId = Guid.NewGuid();
        var message = "Test notification";
        var relatedEntityId = Guid.NewGuid();

        Notification? savedNotification = null;
        _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => savedNotification = n)
            .Returns(Task.CompletedTask);

        await _notificationService.CreateAndSendAsync(userId, "Task", "Title", message, relatedEntityId);

        savedNotification.Should().NotBeNull();
        savedNotification!.Message.Should().Be(message);
        savedNotification.UserId.Should().Be(userId);
        savedNotification.Type.Should().Be("Task");
        savedNotification.RelatedEntityId.Should().Be(relatedEntityId);
        savedNotification.IsRead.Should().BeFalse();

        _realtimeNotifierMock.Verify(r => r.SendToUserAsync(userId, "ReceiveNotification", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldCallRepository()
    {
        var notificationId = Guid.NewGuid();

        _notificationRepositoryMock.Setup(r => r.MarkAsReadAsync(notificationId)).Returns(Task.CompletedTask);

        await _notificationService.MarkAsReadAsync(notificationId);

        _notificationRepositoryMock.Verify(r => r.MarkAsReadAsync(notificationId), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldCallRepository()
    {
        var userId = Guid.NewGuid();

        _notificationRepositoryMock.Setup(r => r.MarkAllAsReadAsync(userId)).Returns(Task.CompletedTask);

        await _notificationService.MarkAllAsReadAsync(userId);

        _notificationRepositoryMock.Verify(r => r.MarkAllAsReadAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnNotifications()
    {
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), Message = "N1" },
            new Notification { Id = Guid.NewGuid(), Message = "N2" }
        };

        _notificationRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, 50)).ReturnsAsync(notifications);

        var result = await _notificationService.GetUserNotificationsAsync(userId, 50);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCount()
    {
        var userId = Guid.NewGuid();
        _notificationRepositoryMock.Setup(r => r.GetUnreadCountAsync(userId)).ReturnsAsync(3);

        var result = await _notificationService.GetUnreadCountAsync(userId);

        result.Should().Be(3);
    }
}

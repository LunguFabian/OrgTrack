using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class ActivityLogServiceTests
{
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly ActivityLogService _activityLogService;

    public ActivityLogServiceTests()
    {
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object);
    }

    [Fact]
    public async Task LogTaskCreatedAsync_ShouldCreateCorrectLogEntry()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        ActivityLog? capturedLog = null;

        _activityLogRepositoryMock.Setup(repo => repo.LogAsync(It.IsAny<ActivityLog>()))
            .Callback<ActivityLog>(log => capturedLog = log)
            .Returns(Task.CompletedTask);

        // Act
        await _activityLogService.LogTaskCreatedAsync(actorId, taskId, "New Task", unitId);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().Be(actorId);
        capturedLog.Action.Should().Be(ActivityLogService.ActionTaskCreated);
        capturedLog.EntityType.Should().Be("TaskItem");
        capturedLog.EntityId.Should().Be(taskId);
        capturedLog.OrganizationUnitId.Should().Be(unitId);
        capturedLog.Details.Should().Contain("New Task");
    }

    [Fact]
    public async Task LogMemberJoinedAsync_ShouldCreateCorrectLogEntry()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        ActivityLog? capturedLog = null;

        _activityLogRepositoryMock.Setup(repo => repo.LogAsync(It.IsAny<ActivityLog>()))
            .Callback<ActivityLog>(log => capturedLog = log)
            .Returns(Task.CompletedTask);

        // Act
        await _activityLogService.LogMemberJoinedAsync(actorId, newMemberId, unitId);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().Be(actorId);
        capturedLog.Action.Should().Be(ActivityLogService.ActionMemberJoined);
        capturedLog.EntityType.Should().Be("User");
        capturedLog.EntityId.Should().Be(newMemberId);
        capturedLog.OrganizationUnitId.Should().Be(unitId);
    }

    [Fact]
    public async Task LogEventCreatedAsync_ShouldCreateCorrectLogEntry()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        ActivityLog? capturedLog = null;

        _activityLogRepositoryMock.Setup(repo => repo.LogAsync(It.IsAny<ActivityLog>()))
            .Callback<ActivityLog>(log => capturedLog = log)
            .Returns(Task.CompletedTask);

        // Act
        await _activityLogService.LogEventCreatedAsync(actorId, eventId, "Important Meeting", unitId);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().Be(actorId);
        capturedLog.Action.Should().Be(ActivityLogService.ActionEventCreated);
        capturedLog.EntityType.Should().Be("Event");
        capturedLog.EntityId.Should().Be(eventId);
        capturedLog.OrganizationUnitId.Should().Be(unitId);
        capturedLog.Details.Should().Contain("Important Meeting");
    }
}

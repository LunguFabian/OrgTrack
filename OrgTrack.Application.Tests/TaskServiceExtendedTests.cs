using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Application.Tests;

public class TaskServiceExtendedTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly ActivityLogService _activityLogService;
    private readonly TaskService _sut;

    public TaskServiceExtendedTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object, new Mock<IUserRepository>().Object);

        var notificationRepoMock = new Mock<INotificationRepository>();
        var realtimeNotifierMock = new Mock<IRealtimeNotifier>();
        var notificationService = new NotificationService(notificationRepoMock.Object, realtimeNotifierMock.Object);

        _sut = new TaskService(
            _taskRepositoryMock.Object,
            _unitRepositoryMock.Object,
            _activityLogService,
            notificationService,
            realtimeNotifierMock.Object
        );
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldThrow_WhenDoneAndTryingToChange()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Status = TaskStatus.Done };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var action = async () => await _sut.UpdateTaskStatusAsync(taskId, TaskStatus.InProgress, Guid.NewGuid(), true);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*marked as 'Done'*");
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldThrow_WhenNoPermission()
    {
        var taskId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Status = TaskStatus.ToDo, AssigneeId = Guid.NewGuid() }; // assigned to someone else
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var action = async () => await _sut.UpdateTaskStatusAsync(taskId, TaskStatus.InProgress, requestingUserId, false);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not assigned to you*");
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateAssignee()
    {
        var taskId = Guid.NewGuid();
        var newAssigneeId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var task = new TaskItem { Id = taskId, Title = "Old", Status = TaskStatus.ToDo, OrganizationUnitId = unitId, AssigneeId = Guid.NewGuid() };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(task)).Returns(Task.CompletedTask);
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(newAssigneeId, unitId)).ReturnsAsync(new UserUnitRole());

        var result = await _sut.UpdateTaskAsync(taskId, "New", "Desc", TaskPriority.High, newAssigneeId, null, requestingUserId, true);

        result.Should().NotBeNull();
        task.AssigneeId.Should().Be(newAssigneeId);
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateParentTask()
    {
        var taskId = Guid.NewGuid();
        var newParentTaskId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var task = new TaskItem { Id = taskId, Title = "Old", Status = TaskStatus.ToDo, OrganizationUnitId = unitId, ParentTaskId = null };
        var newParentTask = new TaskItem { Id = newParentTaskId, OrganizationUnitId = unitId };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(newParentTaskId)).ReturnsAsync(newParentTask);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(task)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateTaskAsync(taskId, "New", "Desc", TaskPriority.High, null, null, requestingUserId, true, newParentTaskId);

        result.Should().NotBeNull();
        task.ParentTaskId.Should().Be(newParentTaskId);
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldThrow_WhenNoPermission()
    {
        var taskId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, CreatorId = Guid.NewGuid() }; // created by someone else
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var action = async () => await _sut.DeleteTaskAsync(taskId, requestingUserId, false);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*delete tasks created by you*");
    }
}

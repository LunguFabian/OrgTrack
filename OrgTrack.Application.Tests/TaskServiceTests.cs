using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Application.Tests;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly ActivityLogService _activityLogService;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object);

        var notificationRepoMock = new Mock<INotificationRepository>();
        var realtimeNotifierMock = new Mock<IRealtimeNotifier>();
        var notificationService = new NotificationService(notificationRepoMock.Object, realtimeNotifierMock.Object);

        _taskService = new TaskService(
            _taskRepositoryMock.Object,
            _unitRepositoryMock.Object,
            _activityLogService,
            notificationService,
            realtimeNotifierMock.Object
        );
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrowException_WhenUnitDoesNotExist()
    {
        var unitId = Guid.NewGuid();
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync((OrganizationUnit?)null);

        var action = async () => await _taskService.CreateTaskAsync(
            "Test Task", "Desc", TaskPriority.Medium, unitId, null, null, Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldUpdateStatus()
    {
        var taskId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Status = TaskStatus.ToDo, AssigneeId = requestingUserId };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(task)).Returns(Task.CompletedTask);

        var result = await _taskService.UpdateTaskStatusAsync(taskId, TaskStatus.InProgress, requestingUserId, true);

        result.Should().NotBeNull();
        task.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateFields()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Title = "Old", Status = TaskStatus.ToDo };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(task)).Returns(Task.CompletedTask);

        var result = await _taskService.UpdateTaskAsync(taskId, "New", "Desc", TaskPriority.High, null, null, Guid.NewGuid(), true);

        result.Should().NotBeNull();
        task.Title.Should().Be("New");
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldDelete()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.DeleteAsync(task)).Returns(Task.CompletedTask);

        await _taskService.DeleteTaskAsync(taskId, Guid.NewGuid(), true);

        _taskRepositoryMock.Verify(r => r.DeleteAsync(task), Times.Once);
    }

    [Fact]
    public async Task GetTasksByUnitAsync_ShouldReturnTasks()
    {
        var unitId = Guid.NewGuid();
        var tasks = new List<TaskItem> { new TaskItem { Id = Guid.NewGuid() } };
        
        _taskRepositoryMock.Setup(r => r.GetByUnitIdAsync(unitId)).ReturnsAsync(tasks);

        var result = await _taskService.GetTasksByUnitAsync(unitId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTasksByAssigneeAsync_ShouldReturnTasks()
    {
        var assigneeId = Guid.NewGuid();
        var tasks = new List<TaskItem> { new TaskItem { Id = Guid.NewGuid() } };
        
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(assigneeId)).ReturnsAsync(tasks);

        var result = await _taskService.GetTasksByAssigneeAsync(assigneeId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }
}

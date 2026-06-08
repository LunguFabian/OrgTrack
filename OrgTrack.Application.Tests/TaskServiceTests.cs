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

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Organization unit not found.");
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrowException_WhenParentTaskNotFound()
    {
        var unitId = Guid.NewGuid();
        var parentTaskId = Guid.NewGuid();
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync(new OrganizationUnit { Id = unitId });
        _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(parentTaskId))
            .ReturnsAsync((TaskItem?)null);

        var action = async () => await _taskService.CreateTaskAsync(
            "Test Task", "Desc", TaskPriority.Medium, unitId, null, null, Guid.NewGuid(), parentTaskId);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Parent task not found.");
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrowException_WhenParentTaskInDifferentUnit()
    {
        var unitId = Guid.NewGuid();
        var parentTaskId = Guid.NewGuid();
        var parentTask = new TaskItem { Id = parentTaskId, OrganizationUnitId = Guid.NewGuid() }; // diff unit

        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync(new OrganizationUnit { Id = unitId });
        _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(parentTaskId))
            .ReturnsAsync(parentTask);

        var action = async () => await _taskService.CreateTaskAsync(
            "Test Task", "Desc", TaskPriority.Medium, unitId, null, null, Guid.NewGuid(), parentTaskId);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Parent task must belong to the same organization unit.");
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrowException_WhenAssigneeNotInUnit()
    {
        var unitId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync(new OrganizationUnit { Id = unitId });
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<UserUnitRole>()); // Empty list -> not a member
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(assigneeId, unitId)).ReturnsAsync((UserUnitRole?)null);
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());

        var action = async () => await _taskService.CreateTaskAsync(
            "Test Task", "Desc", TaskPriority.Medium, unitId, assigneeId, null, Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Assignee must be a member of this organization unit.");
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldCreateTask_Successfully()
    {
        var unitId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync(new OrganizationUnit { Id = unitId });
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(assigneeId, unitId))
            .ReturnsAsync(new UserUnitRole { UserId = assigneeId, OrganizationUnitId = unitId });
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<UserUnitRole> { new UserUnitRole { UserId = assigneeId, OrganizationUnitId = unitId } });
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        
        TaskItem? savedTask = null;
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => { t.Id = Guid.NewGuid(); savedTask = t; })
            .Returns(Task.CompletedTask);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedTask);

        var result = await _taskService.CreateTaskAsync(
            "Test Task", "Desc", TaskPriority.High, unitId, assigneeId, null, creatorId);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Task");
        result.Priority.Should().Be("High");

        savedTask.Should().NotBeNull();
        savedTask!.Title.Should().Be("Test Task");
        savedTask.AssigneeId.Should().Be(assigneeId);
        savedTask.OrganizationUnitId.Should().Be(unitId);
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

    [Fact]
    public async Task GetWorkloadRecommendationAsync_ShouldReturnEmpty_WhenNoMembers()
    {
        var unitId = Guid.NewGuid();
        _unitRepositoryMock.Setup(r => r.GetMembersAsync(unitId))
            .ReturnsAsync(new List<UserUnitRole>()); // Empty members

        var result = await _taskService.GetWorkloadRecommendationAsync(unitId);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkloadRecommendationAsync_ShouldCalculateScoresCorrectly()
    {
        var unitId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var members = new List<UserUnitRole>
        {
            new UserUnitRole { UserId = user1, User = new User { FirstName = "John", LastName = "Doe" } },
            new UserUnitRole { UserId = user2, User = new User { FirstName = "Jane", LastName = "Smith" } }
        };

        var now = DateTime.UtcNow;

        var tasks = new List<TaskItem>
        {
            // User 1: 1 active High priority, 1 completed 5 days ago
            new TaskItem { Id = Guid.NewGuid(), AssigneeId = user1, Status = TaskStatus.InProgress, Priority = TaskPriority.High, SubTasks = new List<TaskItem>() },
            new TaskItem { Id = Guid.NewGuid(), AssigneeId = user1, Status = TaskStatus.Done, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-5) },
            
            // User 2: 2 active Low priority, no completed tasks
            new TaskItem { Id = Guid.NewGuid(), AssigneeId = user2, Status = TaskStatus.ToDo, Priority = TaskPriority.Low, SubTasks = new List<TaskItem>() },
            new TaskItem { Id = Guid.NewGuid(), AssigneeId = user2, Status = TaskStatus.ToDo, Priority = TaskPriority.Low, SubTasks = new List<TaskItem>() }
        };

        _unitRepositoryMock.Setup(r => r.GetMembersAsync(unitId)).ReturnsAsync(members);
        _taskRepositoryMock.Setup(r => r.GetByUnitIdAsync(unitId)).ReturnsAsync(tasks);
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<TaskItem>());

        var result = await _taskService.GetWorkloadRecommendationAsync(unitId);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        // Results are ordered by FinalScore (lowest first) and ranked
        result[0].FinalScore.Should().BeLessThanOrEqualTo(result[1].FinalScore);
        result[0].Rank.Should().Be(1);
        result[1].Rank.Should().Be(2);
        
        // User 2 has no completion time data, should be filled with average
        var user2Result = result.First(r => r.UserId == user2);
        var user1Result = result.First(r => r.UserId == user1);

        user2Result.AvgCompletionTimeRaw.Should().Be(user1Result.AvgCompletionTimeRaw); // Missing value is replaced by average
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldThrowException_WhenTaskIsDone()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Status = TaskStatus.Done };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var action = async () => await _taskService.UpdateTaskAsync(taskId, "Title", "Desc", TaskPriority.Low, null, null, Guid.NewGuid(), true);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Tasks marked as 'Done' cannot be edited.");
    }
}

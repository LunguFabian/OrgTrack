using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Application.DTOs;
using OrgTrack.Api.Models;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using OrgTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class TasksControllerTests : IntegrationTestBase
{
    public TasksControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyTasks_ShouldReturnSuccessAndEmptyList_WhenNoTasksExist()
    {
        // Act
        var response = await Client.GetAsync("/api/me/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTask_ShouldReturnCreated_WhenUserHasPermissions()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Tasks.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var createReq = new CreateTaskRequest("Test Task", "Desc", OrgTrack.Domain.Enums.TaskPriority.High, null, null, null, null);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/organization/units/{unitId}/tasks", createReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnNoContent_WhenUserIsAssignee()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid taskId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Member", Permissions = "[\"Tasks.ViewOwn\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var task = new TaskItem { Id = Guid.NewGuid(), Title = "Task", OrganizationUnitId = unit.Id, CreatorId = _testUserId, AssigneeId = _testUserId, Status = OrgTrack.Domain.Enums.TaskStatus.ToDo };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            unitId = unit.Id;
            taskId = task.Id;
        });

        var updateReq = new UpdateTaskStatusRequest(OrgTrack.Domain.Enums.TaskStatus.InProgress);

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/organization/units/{unitId}/tasks/{taskId}/status", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnTasks_WhenUserHasViewPermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Member", Permissions = "[\"Tasks.View\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var task = new TaskItem { Id = Guid.NewGuid(), Title = "Task", OrganizationUnitId = unit.Id, CreatorId = _testUserId, Status = OrgTrack.Domain.Enums.TaskStatus.ToDo };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        // Act
        var response = await Client.GetAsync($"/api/organization/units/{unitId}/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturnOk_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        Guid taskId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Tasks.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var task = new TaskItem { Id = Guid.NewGuid(), Title = "Task", OrganizationUnitId = unit.Id, CreatorId = _testUserId, Status = OrgTrack.Domain.Enums.TaskStatus.ToDo };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            unitId = unit.Id;
            taskId = task.Id;
        });

        var updateReq = new UpdateTaskRequest("New Title", "New Desc", OrgTrack.Domain.Enums.TaskPriority.Low, null, null, null);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/organization/units/{unitId}/tasks/{taskId}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnNoContent_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        Guid taskId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Tasks.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var task = new TaskItem { Id = Guid.NewGuid(), Title = "Task", OrganizationUnitId = unit.Id, CreatorId = _testUserId, Status = OrgTrack.Domain.Enums.TaskStatus.ToDo };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            unitId = unit.Id;
            taskId = task.Id;
        });

        // Act
        var response = await Client.DeleteAsync($"/api/organization/units/{unitId}/tasks/{taskId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetRecommendations_ShouldReturnOk_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) { db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@aiesec.net" }); }
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Tasks.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        // Act
        var response = await Client.GetAsync($"/api/organization/units/{unitId}/tasks/workload");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Api.Models;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Constants;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/organization/units/{unitId:guid}/tasks")]
public class TasksController(
    TaskService taskService,
    IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTasks(Guid unitId)
    {
        var userId = User.GetUserId();
        
        bool hasManagePermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        bool hasViewPermission   = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksView);
        bool hasViewOwnPermission = await permissionService.HasPermissionAsync(userId, unitId, "Tasks.ViewOwn");
        bool isDirectMember = await permissionService.IsDirectMemberAsync(userId, unitId);
        if (!hasManagePermission && !hasViewPermission && !hasViewOwnPermission && !isDirectMember)
        {
            return Forbid();
        }

        var tasks = await taskService.GetTasksByUnitAsync(unitId);
        if (!hasManagePermission && !hasViewPermission)
        {
            tasks = tasks.Where(t => t.AssigneeId == userId);
        }

        return Ok(tasks);
    }

    [HttpGet("/api/me/tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = User.GetUserId();
        var tasks = await taskService.GetTasksByAssigneeAsync(userId);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(Guid unitId, [FromBody] CreateTaskRequest request)
    {
        var userId = User.GetUserId();
        bool hasManagePermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        bool hasViewOwnPermission = await permissionService.HasPermissionAsync(userId, unitId, "Tasks.ViewOwn");
        
        if (!hasManagePermission && !hasViewOwnPermission)
        {
            return Forbid();
        }
        if (!hasManagePermission)
        {
            request = request with { AssigneeId = userId };
        }

        try
        {
            var task = await taskService.CreateTaskAsync(
                request.Title,
                request.Description,
                request.Priority,
                unitId,
                request.AssigneeId,
                request.Deadline,
                userId,
                request.ParentTaskId,
                request.Status
            );

            return CreatedAtAction(nameof(GetTasks), new { unitId }, task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{taskId:guid}/status")]
    public async Task<IActionResult> UpdateTaskStatus(Guid unitId, Guid taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var userId = User.GetUserId();
        bool hasManagePermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        if (!hasManagePermission 
            && !await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksView)
            && !await permissionService.HasPermissionAsync(userId, unitId, "Tasks.ViewOwn"))
        {
            return Forbid();
        }

        try
        {
            var updatedTask = await taskService.UpdateTaskStatusAsync(taskId, request.NewStatus, userId, hasManagePermission);
            return Ok(updatedTask);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(Guid unitId, Guid taskId, [FromBody] UpdateTaskRequest request)
    {
        var userId = User.GetUserId();
        bool hasManagePermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        bool hasViewOwnPermission = await permissionService.HasPermissionAsync(userId, unitId, "Tasks.ViewOwn");

        if (!hasManagePermission && !hasViewOwnPermission)
        {
            return Forbid();
        }

        try
        {
            var updatedTask = await taskService.UpdateTaskAsync(
                taskId,
                request.Title,
                request.Description,
                request.Priority,
                request.AssigneeId,
                request.Deadline,
                userId,
                hasManagePermission,
                request.ParentTaskId
            );
            return Ok(updatedTask);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid unitId, Guid taskId)
    {
        var userId = User.GetUserId();

        bool hasManagePermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        bool hasViewOwnPermission = await permissionService.HasPermissionAsync(userId, unitId, "Tasks.ViewOwn");

        if (!hasManagePermission && !hasViewOwnPermission)
        {
            return Forbid();
        }

        try
        {
            await taskService.DeleteTaskAsync(taskId, userId, hasManagePermission);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

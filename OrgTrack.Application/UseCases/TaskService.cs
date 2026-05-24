using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Application.UseCases;

public class TaskService(
    ITaskRepository taskRepository,
    IOrganizationUnitRepository unitRepository,
    ActivityLogService activityLogService)
{
    public async Task<TaskDto> CreateTaskAsync(
        string title, string description, TaskPriority priority, 
        Guid unitId, Guid? assigneeId, DateTime? deadline, Guid creatorId)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) throw new ArgumentException("Organization unit not found.");

        if (assigneeId.HasValue)
        {
            var membership = await unitRepository.GetUserUnitRoleAsync(assigneeId.Value, unitId);
            if (membership == null)
                throw new ArgumentException("Assignee must be a member of this organization unit.");
        }

        var task = new TaskItem
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = TaskStatus.ToDo,
            OrganizationUnitId = unitId,
            AssigneeId = assigneeId,
            Deadline = deadline,
            CreatorId = creatorId
        };

        await taskRepository.AddAsync(task);

        var savedTask = await taskRepository.GetByIdAsync(task.Id);
        await activityLogService.LogTaskCreatedAsync(creatorId, task.Id, task.Title, unitId);

        return MapToDto(savedTask!);
    }

    public async Task<IEnumerable<TaskDto>> GetTasksByUnitAsync(Guid unitId)
    {
        var tasks = await taskRepository.GetByUnitIdAsync(unitId);
        return tasks.Select(MapToDto);
    }

    public async Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid assigneeId)
    {
        var tasks = await taskRepository.GetByAssigneeIdAsync(assigneeId);
        return tasks.Select(MapToDto);
    }

    public async Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, TaskStatus newStatus, Guid requestingUserId, bool hasManagePermission)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task == null) throw new ArgumentException("Task not found.");
        
        if (task.Status == TaskStatus.Done && newStatus != TaskStatus.Done)
        {
            throw new InvalidOperationException("Tasks marked as 'Done' can no longer be modified.");
        }

        if (!hasManagePermission)
        {
            if (task.AssigneeId != requestingUserId)
                throw new InvalidOperationException("You cannot modify a task that is not assigned to you.");
            if (newStatus == TaskStatus.Done)
                throw new InvalidOperationException("Only the leader (TL/VP) can approve and mark a task as 'Done'. Move it to 'Review'.");
        }

        var oldStatus = task.Status.ToString();
        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;

        await taskRepository.UpdateAsync(task);
        await activityLogService.LogTaskStatusChangedAsync(
            requestingUserId, task.Id, oldStatus, newStatus.ToString(), task.OrganizationUnitId);
        if (newStatus == TaskStatus.Done)
            await activityLogService.LogTaskDoneAsync(requestingUserId, task.Id, task.Title, task.OrganizationUnitId);

        return MapToDto(task);
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid requestingUserId, bool hasManagePermission)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task == null) throw new ArgumentException("Task not found.");
        
        if (!hasManagePermission && task.CreatorId != requestingUserId)
        {
            throw new InvalidOperationException("You can only delete tasks created by you.");
        }

        await taskRepository.DeleteAsync(task);
    }

    private static TaskDto MapToDto(TaskItem task)
    {
        return new TaskDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.Deadline,
            task.OrganizationUnitId,
            task.Assignee != null ? $"{task.Assignee.FirstName} {task.Assignee.LastName}".Trim() : null,
            task.AssigneeId,
            task.Creator != null ? $"{task.Creator.FirstName} {task.Creator.LastName}".Trim() : "Sistem",
            task.CreatedAt
        );
    }
}

using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Application.UseCases;

public class TaskService(
    ITaskRepository taskRepository,
    IOrganizationUnitRepository unitRepository,
    ActivityLogService activityLogService,
    NotificationService notificationService,
    IRealtimeNotifier realtimeNotifier)
{
    public async Task<TaskDto> CreateTaskAsync(
        string title, string description, TaskPriority priority, 
        Guid unitId, Guid? assigneeId, DateTime? deadline, Guid creatorId, Guid? parentTaskId = null, TaskStatus? initialStatus = null)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) throw new ArgumentException("Organization unit not found.");

        if (assigneeId.HasValue)
        {
            var membership = await unitRepository.GetUserUnitRoleAsync(assigneeId.Value, unitId);
            if (membership == null)
                throw new ArgumentException("Assignee must be a member of this organization unit.");
        }

        if (parentTaskId.HasValue)
        {
            var parentTask = await taskRepository.GetByIdAsync(parentTaskId.Value);
            if (parentTask == null)
                throw new ArgumentException("Parent task not found.");
            if (parentTask.OrganizationUnitId != unitId)
                throw new ArgumentException("Parent task must belong to the same organization unit.");
        }

        var task = new TaskItem
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = initialStatus ?? TaskStatus.ToDo,
            OrganizationUnitId = unitId,
            AssigneeId = assigneeId,
            Deadline = deadline,
            CreatorId = creatorId,
            ParentTaskId = parentTaskId
        };

        await taskRepository.AddAsync(task);

        var savedTask = await taskRepository.GetByIdAsync(task.Id);
        await activityLogService.LogTaskCreatedAsync(creatorId, task.Id, task.Title, unitId);

        var dto = MapToDto(savedTask!);

        // Real-time: notify unit group
        await realtimeNotifier.SendToGroupAsync($"Unit_{unitId}", "TaskCreated", dto);

        // Notification: notify assignee
        if (assigneeId.HasValue && assigneeId.Value != creatorId)
        {
            await notificationService.CreateAndSendAsync(
                assigneeId.Value, "TaskAssigned", "New Task Assigned",
                $"You have been assigned to \"{title}\"",
                task.Id, "Task", creatorId);
        }

        return dto;
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
        var dto = MapToDto(task);

        // Real-time: notify unit group
        await realtimeNotifier.SendToGroupAsync($"Unit_{task.OrganizationUnitId}", "TaskUpdated", dto);

        // Notification: notify assignee if task moved to Done
        if (newStatus == TaskStatus.Done && task.AssigneeId.HasValue && task.AssigneeId.Value != requestingUserId)
        {
            await notificationService.CreateAndSendAsync(
                task.AssigneeId.Value, "TaskStatusChanged", "Task Completed",
                $"Your task \"{task.Title}\" has been approved and marked as Done!",
                task.Id, "Task", requestingUserId);
        }
        // Notification: notify assignee when their task is moved to Review by leader
        else if (newStatus == TaskStatus.WaitingForApproval && task.AssigneeId.HasValue && task.AssigneeId.Value != requestingUserId)
        {
            await notificationService.CreateAndSendAsync(
                task.AssigneeId.Value, "TaskStatusChanged", "Task Status Changed",
                $"Your task \"{task.Title}\" was moved to Review",
                task.Id, "Task", requestingUserId);
        }

        return dto;
    }

    public async Task<TaskDto> UpdateTaskAsync(
        Guid taskId, string title, string description, TaskPriority priority, 
        Guid? assigneeId, DateTime? deadline, Guid requestingUserId, bool hasManagePermission, Guid? parentTaskId = null)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task == null) throw new ArgumentException("Task not found.");

        if (task.Status == TaskStatus.Done)
        {
            throw new InvalidOperationException("Tasks marked as 'Done' cannot be edited.");
        }

        if (!hasManagePermission && task.CreatorId != requestingUserId)
        {
            throw new InvalidOperationException("You can only edit tasks created by you.");
        }

        if (assigneeId.HasValue && assigneeId != task.AssigneeId)
        {
            var membership = await unitRepository.GetUserUnitRoleAsync(assigneeId.Value, task.OrganizationUnitId);
            if (membership == null)
                throw new ArgumentException("Assignee must be a member of this organization unit.");
        }

        if (parentTaskId.HasValue && parentTaskId != task.ParentTaskId)
        {
            if (parentTaskId == taskId)
                throw new ArgumentException("Task cannot be its own parent.");
                
            var parentTask = await taskRepository.GetByIdAsync(parentTaskId.Value);
            if (parentTask == null)
                throw new ArgumentException("Parent task not found.");
            if (parentTask.OrganizationUnitId != task.OrganizationUnitId)
                throw new ArgumentException("Parent task must belong to the same organization unit.");
        }

        var oldAssigneeId = task.AssigneeId;

        task.Title = title;
        task.Description = description;
        task.Priority = priority;
        task.AssigneeId = assigneeId;
        task.Deadline = deadline;
        task.ParentTaskId = parentTaskId;
        task.UpdatedAt = DateTime.UtcNow;

        await taskRepository.UpdateAsync(task);

        var dto = MapToDto(task);

        // Real-time: notify unit group
        await realtimeNotifier.SendToGroupAsync($"Unit_{task.OrganizationUnitId}", "TaskUpdated", dto);

        // Notifications
        if (oldAssigneeId != assigneeId)
        {
            // Notify new assignee
            if (assigneeId.HasValue && assigneeId.Value != requestingUserId)
            {
                await notificationService.CreateAndSendAsync(
                    assigneeId.Value, "TaskAssigned", "Task Reassigned",
                    $"You have been assigned to \"{title}\"",
                    task.Id, "Task", requestingUserId);
            }
            // Notify old assignee
            if (oldAssigneeId.HasValue && oldAssigneeId.Value != requestingUserId)
            {
                await notificationService.CreateAndSendAsync(
                    oldAssigneeId.Value, "TaskUnassigned", "Task Unassigned",
                    $"You are no longer assigned to \"{title}\"",
                    task.Id, "Task", requestingUserId);
            }
        }
        else if (assigneeId.HasValue && assigneeId.Value != requestingUserId)
        {
            // Assignee stayed the same, just notify them of the edit
            await notificationService.CreateAndSendAsync(
                assigneeId.Value, "TaskUpdated", "Task Updated",
                $"The task \"{title}\" has been modified.",
                task.Id, "Task", requestingUserId);
        }

        return dto;
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid requestingUserId, bool hasManagePermission)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task == null) throw new ArgumentException("Task not found.");
        
        if (!hasManagePermission && task.CreatorId != requestingUserId)
        {
            throw new InvalidOperationException("You can only delete tasks created by you.");
        }
        var unitId = task.OrganizationUnitId;
        var taskIdToDelete = task.Id;
        await taskRepository.DeleteAsync(task);

        // Real-time: notify unit group
        await realtimeNotifier.SendToGroupAsync($"Unit_{unitId}", "TaskDeleted", new { id = taskIdToDelete });
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
            task.Assignee?.PictureUrl,
            task.Creator != null ? $"{task.Creator.FirstName} {task.Creator.LastName}".Trim() : "Sistem",
            task.CreatedAt,
            task.ParentTaskId
        );
    }
}

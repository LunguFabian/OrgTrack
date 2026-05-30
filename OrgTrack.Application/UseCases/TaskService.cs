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

        ValidateTaskForUpdate(task, requestingUserId, hasManagePermission);

        if (assigneeId.HasValue && assigneeId != task.AssigneeId)
        {
            await ValidateAssigneeMembershipAsync(assigneeId.Value, task.OrganizationUnitId);
        }

        if (parentTaskId.HasValue && parentTaskId != task.ParentTaskId)
        {
            await ValidateParentTaskAsync(parentTaskId.Value, taskId, task.OrganizationUnitId);
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
        await HandleTaskUpdateNotificationsAsync(task, oldAssigneeId, requestingUserId);

        return dto;
    }

    private void ValidateTaskForUpdate(TaskItem task, Guid requestingUserId, bool hasManagePermission)
    {
        if (task.Status == TaskStatus.Done)
            throw new InvalidOperationException("Tasks marked as 'Done' cannot be edited.");

        if (!hasManagePermission && task.CreatorId != requestingUserId)
            throw new InvalidOperationException("You can only edit tasks created by you.");
    }

    private async Task ValidateAssigneeMembershipAsync(Guid assigneeId, Guid unitId)
    {
        var membership = await unitRepository.GetUserUnitRoleAsync(assigneeId, unitId);
        if (membership == null)
            throw new ArgumentException("Assignee must be a member of this organization unit.");
    }

    private async Task ValidateParentTaskAsync(Guid parentTaskId, Guid currentTaskId, Guid unitId)
    {
        if (parentTaskId == currentTaskId)
            throw new ArgumentException("Task cannot be its own parent.");
            
        var parentTask = await taskRepository.GetByIdAsync(parentTaskId);
        if (parentTask == null)
            throw new ArgumentException("Parent task not found.");
            
        if (parentTask.OrganizationUnitId != unitId)
            throw new ArgumentException("Parent task must belong to the same organization unit.");
    }

    private async Task HandleTaskUpdateNotificationsAsync(TaskItem task, Guid? oldAssigneeId, Guid requestingUserId)
    {
        if (oldAssigneeId != task.AssigneeId)
        {
            // Notify new assignee
            if (task.AssigneeId.HasValue && task.AssigneeId.Value != requestingUserId)
            {
                await notificationService.CreateAndSendAsync(
                    task.AssigneeId.Value, "TaskAssigned", "Task Reassigned",
                    $"You have been assigned to \"{task.Title}\"",
                    task.Id, "Task", requestingUserId);
            }
            // Notify old assignee
            if (oldAssigneeId.HasValue && oldAssigneeId.Value != requestingUserId)
            {
                await notificationService.CreateAndSendAsync(
                    oldAssigneeId.Value, "TaskUnassigned", "Task Unassigned",
                    $"You are no longer assigned to \"{task.Title}\"",
                    task.Id, "Task", requestingUserId);
            }
        }
        else if (task.AssigneeId.HasValue && task.AssigneeId.Value != requestingUserId)
        {
            // Assignee stayed the same, just notify them of the edit
            await notificationService.CreateAndSendAsync(
                task.AssigneeId.Value, "TaskUpdated", "Task Updated",
                $"The task \"{task.Title}\" has been modified.",
                task.Id, "Task", requestingUserId);
        }
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

    public async Task<List<WorkloadScoreDto>> GetWorkloadRecommendationAsync(Guid unitId)
    {
        var members = await unitRepository.GetMembersAsync(unitId);
        if (members == null || !members.Any()) return new List<WorkloadScoreDto>();

        var allTasks = await taskRepository.GetByUnitIdAsync(unitId);
        var tasksList = allTasks.ToList();

        var dtos = CalculateRawScores(members, tasksList);
        
        FillMissingVelocityWithAverage(dtos);

        ApplyNormalizationAndScoring(dtos);

        return dtos.OrderBy(d => d.FinalScore).ToList();
    }

    private static List<WorkloadScoreDto> CalculateRawScores(IEnumerable<UserUnitRole> members, List<TaskItem> tasksList)
    {
        var dtos = new List<WorkloadScoreDto>();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        foreach (var member in members)
        {
            var userTasks = tasksList.Where(t => t.AssigneeId == member.UserId).ToList();

            double currentWorkloadRaw = CalculateCurrentWorkload(userTasks, now, out int subtasksComplexityRaw);
            double velocityDaysRaw = CalculateVelocity(userTasks, thirtyDaysAgo);
            int affinityRaw = userTasks.Count(t => t.Status == TaskStatus.Done && t.UpdatedAt >= thirtyDaysAgo);
            int daysSinceLastAssignmentRaw = CalculateIdleTime(userTasks, now);

            dtos.Add(new WorkloadScoreDto(
                member.UserId,
                $"{member.User?.FirstName} {member.User?.LastName}".Trim(),
                member.User?.PictureUrl,
                0, // Final score calculated later
                currentWorkloadRaw,
                velocityDaysRaw,
                affinityRaw,
                daysSinceLastAssignmentRaw,
                subtasksComplexityRaw
            ));
        }
        return dtos;
    }

    private static double CalculateCurrentWorkload(List<TaskItem> userTasks, DateTime now, out int subtasksComplexityRaw)
    {
        double currentWorkloadRaw = 0;
        subtasksComplexityRaw = 0;
        var activeTasks = userTasks.Where(t => t.Status == TaskStatus.ToDo || t.Status == TaskStatus.InProgress).ToList();

        foreach (var task in activeTasks)
        {
            int priorityPoints = task.Priority switch
            {
                TaskPriority.Low => 1,
                TaskPriority.Medium => 2,
                TaskPriority.High => 3,
                TaskPriority.Critical => 5,
                _ => 2
            };

            double statusFactor = task.Status == TaskStatus.InProgress ? 1.0 : 0.5;
            double deadlinePenalty = (task.Deadline.HasValue && (task.Deadline.Value - now).TotalDays <= 3) ? 2.0 : 0;

            currentWorkloadRaw += (priorityPoints * statusFactor) + deadlinePenalty;
            subtasksComplexityRaw += 1 + task.SubTasks.Count;
        }

        return currentWorkloadRaw;
    }

    private static double CalculateVelocity(List<TaskItem> userTasks, DateTime thirtyDaysAgo)
    {
        var completedTasks = userTasks.Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= thirtyDaysAgo).ToList();
        if (completedTasks.Any())
        {
            return completedTasks.Average(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays);
        }
        return -1; // Flag for missing velocity
    }

    private static int CalculateIdleTime(List<TaskItem> userTasks, DateTime now)
    {
        var lastTask = userTasks.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        if (lastTask != null)
        {
            return (int)(now - lastTask.CreatedAt).TotalDays;
        }
        return 30; // Max out idle time if no tasks ever
    }

    private static void FillMissingVelocityWithAverage(List<WorkloadScoreDto> dtos)
    {
        var membersWithVelocity = dtos.Where(d => d.VelocityDaysRaw >= 0).ToList();
        double teamAverageVelocity = membersWithVelocity.Any() ? membersWithVelocity.Average(d => d.VelocityDaysRaw) : 5.0;

        for (int i = 0; i < dtos.Count; i++)
        {
            if (Math.Abs(dtos[i].VelocityDaysRaw - (-1)) < 0.001)
            {
                dtos[i] = dtos[i] with { VelocityDaysRaw = teamAverageVelocity };
            }
        }
    }

    private static void ApplyNormalizationAndScoring(List<WorkloadScoreDto> dtos)
    {
        double minW = dtos.Any() ? dtos.Min(d => d.CurrentWorkloadRaw) : 0;
        double maxW = dtos.Any() ? dtos.Max(d => d.CurrentWorkloadRaw) : 0;

        double minV = dtos.Any() ? dtos.Min(d => d.VelocityDaysRaw) : 0;
        double maxV = dtos.Any() ? dtos.Max(d => d.VelocityDaysRaw) : 0;

        double minA = dtos.Any() ? dtos.Min(d => d.AffinityRaw) : 0;
        double maxA = dtos.Any() ? dtos.Max(d => d.AffinityRaw) : 0;

        double minF = dtos.Any() ? dtos.Min(d => d.DaysSinceLastAssignmentRaw) : 0;
        double maxF = dtos.Any() ? dtos.Max(d => d.DaysSinceLastAssignmentRaw) : 0;

        double minS = dtos.Any() ? dtos.Min(d => d.SubtasksComplexityRaw) : 0;
        double maxS = dtos.Any() ? dtos.Max(d => d.SubtasksComplexityRaw) : 0;

        for (int i = 0; i < dtos.Count; i++)
        {
            var d = dtos[i];
            
            double normW = Normalize(d.CurrentWorkloadRaw, minW, maxW);
            double normV = Normalize(d.VelocityDaysRaw, minV, maxV);
            double normA = Normalize(d.AffinityRaw, minA, maxA);
            double normF = Normalize(d.DaysSinceLastAssignmentRaw, minF, maxF);
            double normS = Normalize(d.SubtasksComplexityRaw, minS, maxS);

            double finalScore = (0.35 * normW) 
                              + (0.25 * normV) 
                              - (0.15 * normA) 
                              - (0.15 * normF) 
                              + (0.10 * normS);

            dtos[i] = d with { FinalScore = Math.Round(finalScore, 4) };
        }
    }

    private static double Normalize(double value, double min, double max)
    {
        if (Math.Abs(max - min) < 0.001) return 0;
        return (value - min) / (max - min);
    }
}

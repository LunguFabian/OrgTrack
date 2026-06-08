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
        
        if (newStatus == TaskStatus.Done && task.CreatorId != requestingUserId)
        {
            if (task.AssigneeId == requestingUserId)
            {
                throw new InvalidOperationException("Only the creator of the task can approve and mark it as 'Done'. Please move it to 'Review' instead.");
            }
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
        {
            var targetUserId = task.AssigneeId ?? requestingUserId;
            await activityLogService.LogTaskDoneAsync(targetUserId, task.Id, task.Title, task.OrganizationUnitId);
        }
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

    // ──────────────────────────────────────────────────────────────
    //  WORKLOAD RECOMMENDATION ALGORITHM
    //  8-Factor Weighted Scoring Model with Z-Score Normalization
    //
    //  Factors:
    //   W  Current Workload        (↑ = worse)   weight 0.25
    //   V  Avg Completion Time     (↑ = worse)   weight 0.15
    //   T  Throughput              (↑ = better)  weight 0.10
    //   A  Availability            (↑ = better)  weight 0.10
    //   C  Complexity Load         (↑ = worse)   weight 0.08
    //   R  Reliability             (↑ = better)  weight 0.12
    //   D  Overdue Pressure        (↑ = worse)   weight 0.10
    //   X  Cross-Unit Load         (↑ = worse)   weight 0.10
    //
    //  Formula:
    //   S = 0.25·z(W) + 0.15·z(V) − 0.10·z(T) − 0.10·z(A)
    //     + 0.08·z(C) − 0.12·z(R) + 0.10·z(D) + 0.10·z(X)
    //
    //  Lower S → better candidate for task assignment.
    // ──────────────────────────────────────────────────────────────

    private const double WeightWorkload       = 0.25;
    private const double WeightCompletionTime = 0.15;
    private const double WeightThroughput     = 0.10;
    private const double WeightAvailability   = 0.10;
    private const double WeightComplexity     = 0.08;
    private const double WeightReliability    = 0.12;
    private const double WeightOverdue        = 0.10;
    private const double WeightCrossUnit      = 0.10;

    public async Task<List<WorkloadScoreDto>> GetWorkloadRecommendationAsync(Guid unitId)
    {
        var members = await unitRepository.GetMembersAsync(unitId);
        if (members == null || !members.Any()) return new List<WorkloadScoreDto>();

        var unitTasks = (await taskRepository.GetByUnitIdAsync(unitId)).ToList();

        // Pre-fetch cross-unit tasks for each member (single query per member)
        var crossUnitData = new Dictionary<Guid, int>();
        foreach (var member in members)
        {
            var allUserTasks = (await taskRepository.GetByAssigneeIdAsync(member.UserId)).ToList();
            int activeInOtherUnits = allUserTasks.Count(t =>
                t.OrganizationUnitId != unitId &&
                (t.Status == TaskStatus.ToDo || t.Status == TaskStatus.InProgress || t.Status == TaskStatus.WaitingForApproval));
            crossUnitData[member.UserId] = activeInOtherUnits;
        }

        var dtos = CalculateRawScores(members, unitTasks, crossUnitData);
        FillMissingValues(dtos);
        ApplyZScoreNormalizationAndScoring(dtos);

        // Assign ranks after sorting
        var sorted = dtos.OrderBy(d => d.FinalScore).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i] = sorted[i] with { Rank = i + 1 };
        }

        return sorted;
    }

    private static List<WorkloadScoreDto> CalculateRawScores(
        IEnumerable<UserUnitRole> members,
        List<TaskItem> unitTasks,
        Dictionary<Guid, int> crossUnitData)
    {
        var dtos = new List<WorkloadScoreDto>();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        foreach (var member in members)
        {
            var userTasks = unitTasks.Where(t => t.AssigneeId == member.UserId).ToList();

            double workloadRaw = CalculateCurrentWorkload(userTasks, now, out int complexityRaw);
            double avgCompletionTimeRaw = CalculateAvgCompletionTime(userTasks, sixtyDaysAgo);
            int throughputRaw = userTasks.Count(t => t.Status == TaskStatus.Done && t.UpdatedAt >= thirtyDaysAgo);
            int availabilityRaw = CalculateAvailability(userTasks, now);
            double reliabilityRaw = CalculateReliability(userTasks);
            double overduePressureRaw = CalculateOverduePressure(userTasks, now);
            int crossUnitRaw = crossUnitData.GetValueOrDefault(member.UserId, 0);

            dtos.Add(new WorkloadScoreDto(
                member.UserId,
                $"{member.User?.FirstName} {member.User?.LastName}".Trim(),
                member.User?.PictureUrl,
                0,  // Rank assigned after sorting
                0,  // FinalScore calculated after normalization
                workloadRaw,
                avgCompletionTimeRaw,
                throughputRaw,
                availabilityRaw,
                complexityRaw,
                reliabilityRaw,
                overduePressureRaw,
                crossUnitRaw
            ));
        }

        return dtos;
    }

    /// <summary>
    /// Factor W — Current Workload.
    /// Sums priority × status_factor × deadline_multiplier for every active task.
    /// Now includes WaitingForApproval and uses a gradient deadline multiplier.
    /// </summary>
    private static double CalculateCurrentWorkload(List<TaskItem> userTasks, DateTime now, out int complexityLoadRaw)
    {
        double workload = 0;
        complexityLoadRaw = 0;

        var activeTasks = userTasks.Where(t =>
            t.Status == TaskStatus.ToDo ||
            t.Status == TaskStatus.InProgress ||
            t.Status == TaskStatus.WaitingForApproval).ToList();

        foreach (var task in activeTasks)
        {
            int priorityWeight = GetPriorityWeight(task.Priority);

            double statusFactor = task.Status switch
            {
                TaskStatus.InProgress => 1.0,
                TaskStatus.ToDo => 0.5,
                TaskStatus.WaitingForApproval => 0.3,
                _ => 0.5
            };

            double deadlineMultiplier = GetDeadlineMultiplier(task.Deadline, now);

            workload += priorityWeight * statusFactor * deadlineMultiplier;
            complexityLoadRaw += (1 + task.SubTasks.Count) * priorityWeight;
        }

        return workload;
    }

    /// <summary>
    /// Factor V — Average Completion Time (days).
    /// Rolling 60-day window. Returns -1 if no data (handled by FillMissingValues).
    /// </summary>
    private static double CalculateAvgCompletionTime(List<TaskItem> userTasks, DateTime sixtyDaysAgo)
    {
        var completedTasks = userTasks
            .Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= sixtyDaysAgo)
            .ToList();

        if (completedTasks.Any())
        {
            return completedTasks.Average(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays);
        }

        return -1; // Flag for missing data
    }

    /// <summary>
    /// Factor A — Availability (days since last task was assigned).
    /// Higher = more idle = should receive work.
    /// </summary>
    private static int CalculateAvailability(List<TaskItem> userTasks, DateTime now)
    {
        var lastTask = userTasks.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        return lastTask != null
            ? (int)(now - lastTask.CreatedAt).TotalDays
            : 30; // No tasks ever → treat as maximally available
    }

    /// <summary>
    /// Factor R — Reliability Score (0.0 to 1.0).
    /// Ratio of on-time completions to total completions.
    /// A task is on-time if it has no deadline OR was completed before/on the deadline.
    /// Returns -1 if no completed tasks (handled by FillMissingValues).
    /// </summary>
    private static double CalculateReliability(List<TaskItem> userTasks)
    {
        var completedTasks = userTasks.Where(t => t.Status == TaskStatus.Done).ToList();

        if (!completedTasks.Any()) return -1; // Flag for missing data

        int onTime = completedTasks.Count(t =>
            !t.Deadline.HasValue || t.UpdatedAt <= t.Deadline.Value);

        return (double)onTime / completedTasks.Count;
    }

    /// <summary>
    /// Factor D — Overdue Pressure.
    /// Weighted sum of priority × overdue_severity for active tasks past deadline.
    /// Severity = min(days_overdue / 7, 3.0) — capped to avoid extreme outliers.
    /// </summary>
    private static double CalculateOverduePressure(List<TaskItem> userTasks, DateTime now)
    {
        double pressure = 0;

        var activeTasks = userTasks.Where(t =>
            t.Deadline.HasValue &&
            t.Deadline.Value < now &&
            (t.Status == TaskStatus.ToDo || t.Status == TaskStatus.InProgress || t.Status == TaskStatus.WaitingForApproval));

        foreach (var task in activeTasks)
        {
            double daysOverdue = (now - task.Deadline!.Value).TotalDays;
            double severity = Math.Min(daysOverdue / 7.0, 3.0);
            pressure += GetPriorityWeight(task.Priority) * severity;
        }

        return pressure;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static int GetPriorityWeight(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => 1,
        TaskPriority.Medium => 2,
        TaskPriority.High => 3,
        TaskPriority.Critical => 5,
        _ => 2
    };

    /// <summary>
    /// Gradient deadline multiplier replacing the old binary 0/2 penalty.
    /// </summary>
    private static double GetDeadlineMultiplier(DateTime? deadline, DateTime now)
    {
        if (!deadline.HasValue) return 1.0;

        double daysRemaining = (deadline.Value - now).TotalDays;

        return daysRemaining switch
        {
            < 0   => 3.0,   // Overdue
            <= 1  => 2.5,   // Due within 24h
            <= 3  => 2.0,   // Due within 3 days
            <= 7  => 1.5,   // Due within a week
            _     => 1.0    // Comfortable margin
        };
    }

    /// <summary>
    /// Fills missing values (flagged as -1) with team averages.
    /// Applies to both AvgCompletionTime and Reliability.
    /// </summary>
    private static void FillMissingValues(List<WorkloadScoreDto> dtos)
    {
        // Fill missing Avg Completion Time
        var withTime = dtos.Where(d => d.AvgCompletionTimeRaw >= 0).ToList();
        double teamAvgTime = withTime.Any() ? withTime.Average(d => d.AvgCompletionTimeRaw) : 5.0;

        for (int i = 0; i < dtos.Count; i++)
        {
            if (dtos[i].AvgCompletionTimeRaw < 0)
                dtos[i] = dtos[i] with { AvgCompletionTimeRaw = teamAvgTime };
        }

        // Fill missing Reliability
        var withReliability = dtos.Where(d => d.ReliabilityRaw >= 0).ToList();
        double teamAvgReliability = withReliability.Any() ? withReliability.Average(d => d.ReliabilityRaw) : 0.8;

        for (int i = 0; i < dtos.Count; i++)
        {
            if (dtos[i].ReliabilityRaw < 0)
                dtos[i] = dtos[i] with { ReliabilityRaw = teamAvgReliability };
        }
    }

    /// <summary>
    /// Z-Score normalization + weighted scoring.
    /// z(x) = (x - μ) / σ — robust to small team sizes unlike min-max.
    /// </summary>
    private static void ApplyZScoreNormalizationAndScoring(List<WorkloadScoreDto> dtos)
    {
        if (dtos.Count <= 1)
        {
            // Single member: score is always 0, they are the only recommendation
            for (int i = 0; i < dtos.Count; i++)
                dtos[i] = dtos[i] with { FinalScore = 0 };
            return;
        }

        // Extract raw value arrays for each factor
        var wValues = dtos.Select(d => d.CurrentWorkloadRaw).ToArray();
        var vValues = dtos.Select(d => d.AvgCompletionTimeRaw).ToArray();
        var tValues = dtos.Select(d => (double)d.ThroughputRaw).ToArray();
        var aValues = dtos.Select(d => (double)d.AvailabilityDaysRaw).ToArray();
        var cValues = dtos.Select(d => (double)d.ComplexityLoadRaw).ToArray();
        var rValues = dtos.Select(d => d.ReliabilityRaw).ToArray();
        var dValues = dtos.Select(d => d.OverduePressureRaw).ToArray();
        var xValues = dtos.Select(d => (double)d.CrossUnitLoadRaw).ToArray();

        for (int i = 0; i < dtos.Count; i++)
        {
            double zW = ZScore(wValues[i], wValues);
            double zV = ZScore(vValues[i], vValues);
            double zT = ZScore(tValues[i], tValues);
            double zA = ZScore(aValues[i], aValues);
            double zC = ZScore(cValues[i], cValues);
            double zR = ZScore(rValues[i], rValues);
            double zD = ZScore(dValues[i], dValues);
            double zX = ZScore(xValues[i], xValues);

            // S = w1·z(W) + w2·z(V) − w3·z(T) − w4·z(A) + w5·z(C) − w6·z(R) + w7·z(D) + w8·z(X)
            double finalScore =
                  WeightWorkload       * zW
                + WeightCompletionTime * zV
                - WeightThroughput     * zT
                - WeightAvailability   * zA
                + WeightComplexity     * zC
                - WeightReliability    * zR
                + WeightOverdue        * zD
                + WeightCrossUnit      * zX;

            dtos[i] = dtos[i] with { FinalScore = Math.Round(finalScore, 4) };
        }
    }

    /// <summary>
    /// Computes the Z-score of a single value relative to the full dataset.
    /// Returns 0 when standard deviation is ~0 (all values identical).
    /// </summary>
    private static double ZScore(double value, double[] allValues)
    {
        double mean = allValues.Average();
        double variance = allValues.Average(v => (v - mean) * (v - mean));
        double stdDev = Math.Sqrt(variance);

        return stdDev < 0.0001 ? 0 : (value - mean) / stdDev;
    }
}


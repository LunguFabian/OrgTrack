using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

/// <summary>
/// Service dedicated to logging important actions in ActivityLogs.
/// Injected into any other Service that wants to log an action.
/// Helper methods (LogTaskCompleted, LogAttendanceConfirmed, etc.) 
/// provide a clear API and prevent typos on Action strings.
/// </summary>
public class ActivityLogService(IActivityLogRepository activityLogRepository, IUserRepository userRepository)
{
    public const string ActionTaskCreated          = "TaskCreated";
    public const string ActionTaskStatusChanged    = "TaskStatusChanged";
    public const string ActionTaskDone             = "TaskDone";
    public const string ActionMemberJoined         = "MemberJoined";
    public const string ActionMemberRemoved        = "MemberRemoved";
    public const string ActionEventCreated         = "EventCreated";
    public const string ActionAttendanceConfirmed  = "AttendanceConfirmed";
    public const string ActionInviteLinkUsed       = "InviteLinkUsed";
    public const string ActionUnitCreated          = "UnitCreated";
    public Task LogTaskCreatedAsync(Guid actorId, Guid taskId, string taskTitle, Guid unitId)
        => WriteLog(actorId, ActionTaskCreated, "TaskItem", taskId, unitId,
            $"Task created: \"{taskTitle}\"");

    public Task LogTaskStatusChangedAsync(Guid actorId, Guid taskId, string oldStatus, string newStatus, Guid unitId)
        => WriteLog(actorId, ActionTaskStatusChanged, "TaskItem", taskId, unitId,
            $"Status changed: {oldStatus} → {newStatus}");

    public Task LogTaskDoneAsync(Guid actorId, Guid taskId, string taskTitle, Guid unitId)
        => WriteLog(actorId, ActionTaskDone, "TaskItem", taskId, unitId,
            $"Task completed: \"{taskTitle}\"");

    public async Task LogMemberJoinedAsync(Guid actorId, Guid newMemberId, Guid unitId)
    {
        var user = await userRepository.GetByIdAsync(newMemberId);
        var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : newMemberId.ToString();
        await WriteLog(actorId, ActionMemberJoined, "User", newMemberId, unitId, $"Member {name} was added to the unit");
    }

    public async Task LogMemberRemovedAsync(Guid actorId, Guid removedMemberId, Guid unitId)
    {
        var user = await userRepository.GetByIdAsync(removedMemberId);
        var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : removedMemberId.ToString();
        await WriteLog(actorId, ActionMemberRemoved, "User", removedMemberId, unitId, $"Member {name} was removed from the unit");
    }

    public Task LogEventCreatedAsync(Guid actorId, Guid eventId, string eventTitle, Guid unitId)
        => WriteLog(actorId, ActionEventCreated, "Event", eventId, unitId,
            $"Event created: \"{eventTitle}\"");

    public async Task LogAttendanceConfirmedAsync(Guid actorId, Guid targetUserId, Guid eventId, string status, Guid unitId)
    {
        var user = await userRepository.GetByIdAsync(targetUserId);
        var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : targetUserId.ToString();
        await WriteLog(actorId, ActionAttendanceConfirmed, "Event", eventId, unitId, $"Attendance for user {name} set to: {status}");
    }

    public Task LogInviteLinkUsedAsync(Guid newMemberId, Guid unitId)
        => WriteLog(newMemberId, ActionInviteLinkUsed, "InviteLink", unitId, unitId,
            "Joined the team via an invite link");

    public Task LogUnitCreatedAsync(Guid actorId, Guid unitId, string unitName)
        => WriteLog(actorId, ActionUnitCreated, "OrganizationUnit", unitId, unitId,
            $"Unit created: \"{unitName}\"");
    private Task WriteLog(Guid userId, string action, string entityType, Guid entityId,
        Guid? unitId, string? details = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OrganizationUnitId = unitId,
            Details = details
        };
        return activityLogRepository.LogAsync(log);
    }
}

using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

/// <summary>
/// Service dedicated to logging important actions in ActivityLogs.
/// Injected into any other Service that wants to log an action.
/// Helper methods (LogTaskCompleted, LogAttendanceConfirmed, etc.) 
/// provide a clear API and prevent typos on Action strings.
/// </summary>
public class ActivityLogService(IActivityLogRepository activityLogRepository)
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
            $"Task creat: \"{taskTitle}\"");

    public Task LogTaskStatusChangedAsync(Guid actorId, Guid taskId, string oldStatus, string newStatus, Guid unitId)
        => WriteLog(actorId, ActionTaskStatusChanged, "TaskItem", taskId, unitId,
            $"Status schimbat: {oldStatus} → {newStatus}");

    public Task LogTaskDoneAsync(Guid actorId, Guid taskId, string taskTitle, Guid unitId)
        => WriteLog(actorId, ActionTaskDone, "TaskItem", taskId, unitId,
            $"Task finalizat: \"{taskTitle}\"");

    public Task LogMemberJoinedAsync(Guid actorId, Guid newMemberId, Guid unitId)
        => WriteLog(actorId, ActionMemberJoined, "User", newMemberId, unitId,
            $"Membrul {newMemberId} a fost adăugat în unitate");

    public Task LogMemberRemovedAsync(Guid actorId, Guid removedMemberId, Guid unitId)
        => WriteLog(actorId, ActionMemberRemoved, "User", removedMemberId, unitId,
            $"Membrul {removedMemberId} a fost eliminat din unitate");

    public Task LogEventCreatedAsync(Guid actorId, Guid eventId, string eventTitle, Guid unitId)
        => WriteLog(actorId, ActionEventCreated, "Event", eventId, unitId,
            $"Eveniment creat: \"{eventTitle}\"");

    public Task LogAttendanceConfirmedAsync(Guid actorId, Guid targetUserId, Guid eventId, string status, Guid unitId)
        => WriteLog(actorId, ActionAttendanceConfirmed, "Event", eventId, unitId,
            $"Prezența utilizatorului {targetUserId} setată la: {status}");

    public Task LogInviteLinkUsedAsync(Guid newMemberId, Guid unitId)
        => WriteLog(newMemberId, ActionInviteLinkUsed, "InviteLink", unitId, unitId,
            "S-a alăturat echipei printr-un link de invitație");

    public Task LogUnitCreatedAsync(Guid actorId, Guid unitId, string unitName)
        => WriteLog(actorId, ActionUnitCreated, "OrganizationUnit", unitId, unitId,
            $"Unitate creată: \"{unitName}\"");
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

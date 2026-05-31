using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Infrastructure.Persistence;

public class AnalyticsService(
    IActivityLogRepository activityLogRepository,
    IOrganizationUnitRepository unitRepository,
    OrgTrackDbContext context)
{
    /// <summary>
    /// Calculates the activity score of a volunteer.
    /// Formula: TasksDone * 3 + EventsAttended * 1
    /// </summary>
    public async Task<MemberActivityScoreDto> GetMemberScoreAsync(Guid userId)
    {
        var logs = await activityLogRepository.GetByUserIdAsync(userId, limit: 1000);

        var tasksDone = logs.Count(l => l.Action == ActivityLogService.ActionTaskDone);
        var eventsAttended = logs.Count(l =>
            l.Action == ActivityLogService.ActionAttendanceConfirmed &&
            l.Details != null && l.Details.Contains("Present"));
        var user = await context.Users.FindAsync(userId);
        var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown";

        return new MemberActivityScoreDto(
            userId,
            name,
            TasksDone: tasksDone,
            EventsAttended: eventsAttended,
            TotalScore: tasksDone * 3 + eventsAttended * 1
        );
    }

    /// <summary>
    /// Returns an activity summary for an organizational unit.
    /// </summary>
    public async Task<UnitActivitySummaryDto> GetUnitSummaryAsync(Guid unitId)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) throw new ArgumentException("Unit not found.");

        var descendantIds = await unitRepository.GetDescendantUnitIdsAsync(unitId);
        var allIds = new List<Guid> { unitId };
        allIds.AddRange(descendantIds);

        var logs = await activityLogRepository.GetByUnitIdsAsync(allIds, limit: 1000);

        var tasksDone = await context.Tasks.CountAsync(t => allIds.Contains(t.OrganizationUnitId) && t.Status == TaskStatus.Done);
        var eventsHeld = await context.Events.CountAsync(e => allIds.Contains(e.OrganizationUnitId));
        var membersActive = await context.UserUnitRoles.CountAsync(m => allIds.Contains(m.OrganizationUnitId));
        
        var recentLogs = logs
            .Take(20)
            .Select(l => {
                var formattedAction = System.Text.RegularExpressions.Regex.Replace(l.Action, "(?<!^)([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));
                var details = l.Details;
                if (details != null)
                {
                    details = details.Replace("Task creat:", "Task created:")
                                     .Replace("Status schimbat:", "Status changed:")
                                     .Replace("Task finalizat:", "Task completed:")
                                     .Replace("Membrul ", "Member ")
                                     .Replace(" a fost adăugat în unitate", " was added to the unit")
                                     .Replace(" a fost eliminat din unitate", " was removed from the unit")
                                     .Replace("Eveniment creat:", "Event created:")
                                     .Replace("Prezența utilizatorului ", "Attendance for user ")
                                     .Replace(" setată la:", " set to:")
                                     .Replace("S-a alăturat echipei printr-un link de invitație", "Joined the team via an invite link")
                                     .Replace("Unitate creată:", "Unit created:");
                }
                return new ActivityLogDto(l.CreatedAt, formattedAction, l.EntityType, details, l.OrganizationUnit?.Name);
            });

        return new UnitActivitySummaryDto(
            unitId,
            unit.Name,
            tasksDone,
            eventsHeld,
            membersActive,
            recentLogs
        );
    }

    /// <summary>
    /// National dashboard: the aggregate scores of all descendant units from the national node.
    /// </summary>
    public async Task<IEnumerable<UnitActivitySummaryDto>> GetNationalDashboardAsync(Guid nationalUnitId)
    {
        var allUnits = await context.OrganizationUnits
            .Where(ou => ou.ParentUnitId == nationalUnitId)
            .ToListAsync();

        var summaries = new List<UnitActivitySummaryDto>();
        foreach (var unit in allUnits)
        {
            summaries.Add(await GetUnitSummaryAsync(unit.Id));
        }

        return summaries.OrderByDescending(s => s.TasksDone + s.EventsHeld);
    }
}

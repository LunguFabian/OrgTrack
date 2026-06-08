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

    /// <summary>
    /// Calculates the 5-Factor Burnout Risk for all members the requester has hierarchical visibility over.
    /// </summary>
    public async Task<List<BurnoutRiskDto>> GetHierarchicalBurnoutRisksAsync(Guid requesterId, Guid? targetUnitId = null)
    {
        // 1. Determine hierarchical visibility
        var requesterRoles = await context.UserUnitRoles
            .Include(r => r.Role)
            .Where(r => r.UserId == requesterId && r.Role != null && r.Role.Name != "Member")
            .ToListAsync();

        if (!requesterRoles.Any()) return new List<BurnoutRiskDto>(); // Not a leader

        var visibleUnitIds = new HashSet<Guid>();
        
        foreach (var role in requesterRoles)
        {
            var unitId = role.OrganizationUnitId;
            visibleUnitIds.Add(unitId);
            var descendants = await unitRepository.GetDescendantUnitIdsAsync(unitId);
            foreach (var d in descendants) visibleUnitIds.Add(d);
        }

        if (targetUnitId.HasValue)
        {
            if (!visibleUnitIds.Contains(targetUnitId.Value)) return new List<BurnoutRiskDto>();
            // Focus only on target unit and its descendants
            var targetDescendants = await unitRepository.GetDescendantUnitIdsAsync(targetUnitId.Value);
            visibleUnitIds.Clear();
            visibleUnitIds.Add(targetUnitId.Value);
            foreach(var d in targetDescendants) visibleUnitIds.Add(d);
        }

        // 2. Get target users
        var targetUsers = await context.UserUnitRoles
            .Include(r => r.User)
            .Where(r => visibleUnitIds.Contains(r.OrganizationUnitId) && r.UserId != requesterId)
            .Select(r => r.User)
            .Distinct()
            .ToListAsync();

        var results = new List<BurnoutRiskDto>();
        var now = DateTime.UtcNow;
        var recentWindow = now.AddDays(-14);
        var baselineStart = now.AddDays(-60);

        foreach (var user in targetUsers)
        {
            if (user == null) continue;
            
            var tasks = await context.Tasks
                .Where(t => t.AssigneeId == user.Id)
                .ToListAsync();

            var logs = await context.ActivityLogs
                .Where(a => a.UserId == user.Id && a.CreatedAt >= baselineStart)
                .ToListAsync();

            var rsvps = await context.EventRsvps
                .Include(r => r.Event)
                .Where(r => r.UserId == user.Id && r.Event != null && r.Event.StartDate <= now)
                .OrderByDescending(r => r.Event!.StartDate)
                .Take(5)
                .ToListAsync();

            // Factor 1: Velocity Degradation (25%)
            var recentCompleted = tasks.Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= recentWindow).ToList();
            var baselineCompleted = tasks.Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= baselineStart && t.UpdatedAt < recentWindow).ToList();
            
            double recentAvg = recentCompleted.Any() ? recentCompleted.Average(t => ((t.UpdatedAt ?? DateTime.UtcNow) - t.CreatedAt).TotalDays) : 1.0;
            double baselineAvg = baselineCompleted.Any() ? baselineCompleted.Average(t => ((t.UpdatedAt ?? DateTime.UtcNow) - t.CreatedAt).TotalDays) : 1.0;
            
            double velocityRatio = baselineAvg > 0 ? recentAvg / baselineAvg : 1.0;
            double f1Score = Math.Min(100, Math.Max(0, (velocityRatio - 1.0) * 50)); // If ratio 1.5 -> 25%. If 3.0 -> 100%
            
            // Factor 2: Chronic Overdue Ratio (25%)
            var last30DaysCompleted = tasks.Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= now.AddDays(-30)).ToList();
            int pastDeadlineCount = last30DaysCompleted.Count(t => t.Deadline.HasValue && t.UpdatedAt > t.Deadline.Value);
            int currentOverdue = tasks.Count(t => t.Status != TaskStatus.Done && t.Deadline.HasValue && t.Deadline.Value < now);
            int totalLate = pastDeadlineCount + currentOverdue;
            int totalActiveAndRecent = last30DaysCompleted.Count + tasks.Count(t => t.Status != TaskStatus.Done);
            
            double overdueRatio = totalActiveAndRecent > 0 ? (double)totalLate / totalActiveAndRecent : 0;
            double f2Score = Math.Min(100, overdueRatio * 200); // 50% late = 100 score
            
            // Factor 3: Task Churn / Frustration Index (20%)
            var statusChanges = logs.Where(l => l.Action == "Status schimbat:" || l.Action == "Status changed:").ToList();
            int activeTaskCount = tasks.Count(t => t.Status != TaskStatus.Done);
            double churnRate = activeTaskCount > 0 ? (double)statusChanges.Count / activeTaskCount : 0;
            double f3Score = Math.Min(100, churnRate * 20); // 5 changes per task = 100 score

            // Factor 4: Workload Density Spike (15%)
            double currentScore = tasks.Where(t => t.Status != TaskStatus.Done).Sum(t => t.Priority == OrgTrack.Domain.Enums.TaskPriority.High ? 3 : (t.Priority == OrgTrack.Domain.Enums.TaskPriority.Medium ? 2 : 1));
            double baselineScore = baselineCompleted.Sum(t => t.Priority == OrgTrack.Domain.Enums.TaskPriority.High ? 3 : (t.Priority == OrgTrack.Domain.Enums.TaskPriority.Medium ? 2 : 1)) / 6.0; // Weekly avg over 6 weeks
            
            double spikeRatio = baselineScore > 0 ? currentScore / baselineScore : 1.0;
            double f4Score = Math.Min(100, Math.Max(0, (spikeRatio - 1.0) * 33)); // 4x spike = 100 score

            // Factor 5: Absenteeism Streak (15%)
            int consecutiveAbsences = 0;
            foreach(var rsvp in rsvps)
            {
                if (rsvp.Status == OrgTrack.Domain.Enums.PresenceStatus.Absent) consecutiveAbsences++;
                else break; // Streak broken
            }
            double f5Score = Math.Min(100, consecutiveAbsences * 33.3); // 3 absences = 100 score

            // Calculate Final Weighted Score
            double finalScore = (f1Score * 0.25) + (f2Score * 0.25) + (f3Score * 0.20) + (f4Score * 0.15) + (f5Score * 0.15);
            
            var flags = new List<string>();
            if (f1Score > 50) flags.Add($"Velocity degraded. Taking {(velocityRatio).ToString("0.1")}x longer to finish tasks.");
            if (f2Score > 50) flags.Add($"High overdue rate ({(overdueRatio*100).ToString("0")}% of recent work is late).");
            if (f3Score > 50) flags.Add($"Task frustration detected ({churnRate.ToString("0.1")} status bounces per active task).");
            if (f4Score > 50) flags.Add($"Workload spiked {(spikeRatio).ToString("0.1")}x above historical capacity.");
            if (f5Score >= 60) flags.Add($"{consecutiveAbsences} consecutive event absences.");

            string riskLevel = "Healthy";
            if (finalScore >= 75) riskLevel = "Critical";
            else if (finalScore >= 50) riskLevel = "High";
            else if (finalScore >= 25) riskLevel = "Elevated";

            results.Add(new BurnoutRiskDto(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.PictureUrl,
                Math.Round(finalScore, 1),
                riskLevel,
                flags
            ));
        }

        return results.OrderByDescending(r => r.BurnoutScorePercentage).ToList();
    }
}

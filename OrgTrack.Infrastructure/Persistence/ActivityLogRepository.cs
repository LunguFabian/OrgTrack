using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class ActivityLogRepository(OrgTrackDbContext context) : IActivityLogRepository
{
    public async Task LogAsync(ActivityLog log)
    {
        context.ActivityLogs.Add(log);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByUnitIdAsync(Guid unitId, int limit = 50)
    {
        return await context.ActivityLogs
            .Include(l => l.User)
            .Where(l => l.OrganizationUnitId == unitId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByUnitIdsAsync(IEnumerable<Guid> unitIds, int limit = 50)
    {
        var ids = unitIds.ToList();
        return await context.ActivityLogs
            .Include(l => l.User)
            .Include(l => l.OrganizationUnit)
            .Where(l => l.OrganizationUnitId.HasValue && ids.Contains(l.OrganizationUnitId.Value))
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await context.ActivityLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

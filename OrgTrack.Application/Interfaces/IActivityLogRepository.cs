using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface IActivityLogRepository
{
    Task LogAsync(ActivityLog log);
    Task<IEnumerable<ActivityLog>> GetByUnitIdAsync(Guid unitId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetByUnitIdsAsync(IEnumerable<Guid> unitIds, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50);
}

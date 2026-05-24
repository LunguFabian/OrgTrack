using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<TaskItem>> GetByUnitIdAsync(Guid unitId);
    Task<IEnumerable<TaskItem>> GetByAssigneeIdAsync(Guid assigneeId);
    
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(TaskItem task);
}

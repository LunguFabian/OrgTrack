using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class TaskRepository(OrgTrackDbContext context) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return await context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.OrganizationUnit)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskItem>> GetByUnitIdAsync(Guid unitId)
    {
        return await context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Where(t => t.OrganizationUnitId == unitId)
            .OrderBy(t => t.Deadline ?? DateTime.MaxValue) 
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByAssigneeIdAsync(Guid assigneeId)
    {
        return await context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.OrganizationUnit)
            .Where(t => t.AssigneeId == assigneeId)
            .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
            .ToListAsync();
    }

    public async Task AddAsync(TaskItem task)
    {
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        context.Tasks.Update(task);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TaskItem task)
    {
        context.Tasks.Remove(task);
        await context.SaveChangesAsync();
    }
}

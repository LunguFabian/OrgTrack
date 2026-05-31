using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Infrastructure.Persistence;

public class OrganizationUnitRepository(OrgTrackDbContext context) : IOrganizationUnitRepository
{
    public async Task<OrganizationUnit?> GetByIdWithChildrenAsync(Guid id)
    {
        return await context.OrganizationUnits
            .Include(u => u.ChildUnits!)
                .ThenInclude(c => c.Members)
                    .ThenInclude(m => m.User)
            .Include(u => u.ChildUnits!)
                .ThenInclude(c => c.Members)
                    .ThenInclude(m => m.Role)
            .Include(u => u.Members)
                .ThenInclude(m => m.User)
            .Include(u => u.Members)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<OrganizationUnit?> GetByIdAsync(Guid id)
    {
        return await context.OrganizationUnits.FindAsync(id);
    }

    public async Task<List<OrganizationUnit>> GetRootUnitsAsync()
    {
        return await context.OrganizationUnits
            .Where(u => u.ParentUnitId == null)
            .Include(u => u.ChildUnits)
            .ToListAsync();
    }

    public async Task<List<OrganizationUnit>> GetAllUnitsAsync()
    {
        return await context.OrganizationUnits.ToListAsync();
    }

    public async Task<HashSet<Guid>> GetAncestorUnitIdsAsync(Guid startUnitId)
    {
        var allUnits = await GetAllUnitsAsync();
        var map = allUnits.ToDictionary(u => u.Id, u => u.ParentUnitId);
        
        var ancestors = new HashSet<Guid>();
        Guid? currentId = startUnitId;
        
        while (currentId.HasValue && map.TryGetValue(currentId.Value, out var parentId))
        {
            ancestors.Add(currentId.Value);
            currentId = parentId;
        }
        
        return ancestors;
    }

    public async Task<HashSet<Guid>> GetDescendantUnitIdsAsync(Guid startUnitId)
    {
        var allUnits = await GetAllUnitsAsync();
        var childrenMap = allUnits
            .Where(u => u.ParentUnitId.HasValue)
            .GroupBy(u => u.ParentUnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(u => u.Id).ToList());

        var descendants = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(startUnitId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            descendants.Add(currentId);

            if (childrenMap.TryGetValue(currentId, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return descendants;
    }

    public async Task<List<OrganizationUnit>> GetChildrenAsync(Guid parentId)
    {
        return await context.OrganizationUnits
            .Where(u => u.ParentUnitId == parentId)
            .Include(u => u.ChildUnits)
            .ToListAsync();
    }

    public async Task<List<OrganizationUnit>> GetByDepartmentTypeAsync(DepartmentType departmentType)
    {
        return await context.OrganizationUnits
            .Where(u => u.DepartmentType == departmentType)
            .Include(u => u.ChildUnits)
            .Include(u => u.Members)
                .ThenInclude(m => m.User)
            .ToListAsync();
    }

    public async Task AddAsync(OrganizationUnit unit)
    {
        context.OrganizationUnits.Add(unit);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OrganizationUnit unit)
    {
        context.OrganizationUnits.Update(unit);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(OrganizationUnit unit)
    {
        context.OrganizationUnits.Remove(unit);
        await context.SaveChangesAsync();
    }

    public async Task<List<UserUnitRole>> GetMembersAsync(Guid unitId)
    {
        return await context.UserUnitRoles
            .Where(m => m.OrganizationUnitId == unitId)
            .Include(m => m.User)
            .Include(m => m.Role)
            .Include(m => m.OrganizationUnit)
            .ToListAsync();
    }

    public async Task<List<UserUnitRole>> GetMembersForUnitsAsync(IEnumerable<Guid> unitIds)
    {
        var unitIdList = unitIds.ToList();
        return await context.UserUnitRoles
            .Where(m => unitIdList.Contains(m.OrganizationUnitId))
            .Include(m => m.User)
            .Include(m => m.Role)
            .Include(m => m.OrganizationUnit)
            .ToListAsync();
    }

    public async Task AddMemberAsync(UserUnitRole userUnitRole)
    {
        context.UserUnitRoles.Add(userUnitRole);
        await context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(UserUnitRole userUnitRole)
    {
        context.UserUnitRoles.Remove(userUnitRole);
        await context.SaveChangesAsync();
    }

    public async Task UpdateMemberAsync(UserUnitRole userUnitRole)
    {
        context.UserUnitRoles.Update(userUnitRole);
        await context.SaveChangesAsync();
    }

    public async Task<UserUnitRole?> GetUserUnitRoleAsync(Guid userId, Guid unitId)
    {
        return await context.UserUnitRoles
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationUnitId == unitId);
    }

    public async Task<List<UserUnitRole>> GetUserRolesAsync(Guid userId)
    {
        return await context.UserUnitRoles
            .Include(uur => uur.Role)
            .Include(uur => uur.OrganizationUnit)
            .Where(uur => uur.UserId == userId)
            .ToListAsync();
    }
}

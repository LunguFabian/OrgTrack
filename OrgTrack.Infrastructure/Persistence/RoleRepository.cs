using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class RoleRepository(OrgTrackDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await context.Roles.FindAsync(id);
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await context.Roles.ToListAsync();
    }

    public async Task AddAsync(Role role)
    {
        context.Roles.Add(role);
        await context.SaveChangesAsync();
    }
}

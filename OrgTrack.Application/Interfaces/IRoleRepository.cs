using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
    Task<Role?> GetByIdAsync(Guid id);
    Task<List<Role>> GetAllAsync();
    Task AddAsync(Role role);
}

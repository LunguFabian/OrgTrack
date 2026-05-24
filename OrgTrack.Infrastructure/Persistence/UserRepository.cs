using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class UserRepository(OrgTrackDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query)
    {
        var lowerQuery = query.ToLower();
        return await context.Users
            .Where(u => u.FirstName.ToLower().Contains(lowerQuery) 
                     || u.LastName.ToLower().Contains(lowerQuery)
                     || u.Email.ToLower().Contains(lowerQuery))
            .Take(20)
            .ToListAsync();
    }
}
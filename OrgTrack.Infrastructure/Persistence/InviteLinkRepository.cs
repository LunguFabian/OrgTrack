using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class InviteLinkRepository(OrgTrackDbContext context) : IInviteLinkRepository
{
    public async Task<InviteLink?> GetByTokenAsync(string token)
    {
        return await context.InviteLinks
            .Include(i => i.OrganizationUnit)
            .Include(i => i.Role)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task AddAsync(InviteLink inviteLink)
    {
        context.InviteLinks.Add(inviteLink);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InviteLink inviteLink)
    {
        context.InviteLinks.Update(inviteLink);
        await context.SaveChangesAsync();
    }
}

using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface IInviteLinkRepository
{
    Task<InviteLink?> GetByTokenAsync(string token);
    Task AddAsync(InviteLink inviteLink);
    Task UpdateAsync(InviteLink inviteLink);
}

using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

public class InviteLinkService(
    IInviteLinkRepository inviteLinkRepository,
    IOrganizationUnitRepository unitRepository,
    IRoleRepository roleRepository,
    ActivityLogService activityLogService)
{
    public async Task<InviteLinkCreatedDto> GenerateLinkAsync(
        Guid creatorUserId, Guid unitId, string roleName, int hoursValid, int? maxUses)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) throw new ArgumentException("Organization unit not found.");
        var role = await roleRepository.GetByNameAsync(roleName);
        if (role == null) throw new ArgumentException($"Role '{roleName}' is not valid.");

        var invite = new InviteLink
        {
            Token = Guid.NewGuid().ToString("N"),
            OrganizationUnitId = unit.Id,
            RoleId = role.Id,
            CreatedByUserId = creatorUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(hoursValid),
            MaxUses = maxUses,
            IsActive = true
        };

        await inviteLinkRepository.AddAsync(invite);
        var baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
        var frontendUrl = $"{baseUrl.TrimEnd('/')}/invite/{invite.Token}";

        return new InviteLinkCreatedDto(invite.Token, frontendUrl, invite.ExpiresAt);
    }

    public async Task<InviteLinkPreviewDto?> GetInviteDetailsAsync(string token)
    {
        var invite = await inviteLinkRepository.GetByTokenAsync(token);
        if (invite == null) return null;

        var isExpired = !invite.IsActive || invite.ExpiresAt < DateTime.UtcNow;
        var isMaxUsesReached = invite.MaxUses.HasValue && invite.CurrentUses >= invite.MaxUses.Value;

        var creatorName = $"{invite.CreatedByUser?.FirstName} {invite.CreatedByUser?.LastName}".Trim();

        return new InviteLinkPreviewDto(
            invite.OrganizationUnit?.Name ?? "Necunoscut",
            invite.Role?.Name ?? "Necunoscut",
            creatorName,
            invite.ExpiresAt,
            isExpired,
            isMaxUsesReached
        );
    }

    public async Task<bool> JoinViaLinkAsync(string token, Guid userId)
    {
        var invite = await inviteLinkRepository.GetByTokenAsync(token);
        if (invite == null) throw new ArgumentException("Invalid or non-existent invite link.");
        if (!invite.IsActive || invite.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("This invite link has expired.");

        if (invite.MaxUses.HasValue && invite.CurrentUses >= invite.MaxUses.Value)
            throw new InvalidOperationException("This link has reached the maximum allowed uses.");
        var existingMembership = await unitRepository.GetUserUnitRoleAsync(userId, invite.OrganizationUnitId);
        if (existingMembership != null)
            throw new InvalidOperationException("You are already part of this organization unit.");
        var newMember = new UserUnitRole
        {
            UserId = userId,
            OrganizationUnitId = invite.OrganizationUnitId,
            RoleId = invite.RoleId
        };

        await unitRepository.AddMemberAsync(newMember);
        invite.CurrentUses++;
        await inviteLinkRepository.UpdateAsync(invite);
        await activityLogService.LogInviteLinkUsedAsync(userId, invite.OrganizationUnitId);

        return true;
    }
}

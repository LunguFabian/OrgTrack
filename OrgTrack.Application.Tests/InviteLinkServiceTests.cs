using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using Xunit;

namespace OrgTrack.Application.Tests;

public class InviteLinkServiceTests
{
    private readonly Mock<IInviteLinkRepository> _inviteLinkRepositoryMock;
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly InviteLinkService _inviteLinkService;

    public InviteLinkServiceTests()
    {
        _inviteLinkRepositoryMock = new Mock<IInviteLinkRepository>();
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        
        var activityLogRepoMock = new Mock<IActivityLogRepository>();
        var activityLogService = new ActivityLogService(activityLogRepoMock.Object);

        _inviteLinkService = new InviteLinkService(
            _inviteLinkRepositoryMock.Object,
            _unitRepositoryMock.Object,
            _roleRepositoryMock.Object,
            activityLogService
        );
    }

    [Fact]
    public async Task GenerateLinkAsync_ShouldGenerateLink()
    {
        var unitId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit { Id = unitId });
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("Member")).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "Member" });
        _inviteLinkRepositoryMock.Setup(r => r.AddAsync(It.IsAny<InviteLink>())).Returns(Task.CompletedTask);

        var result = await _inviteLinkService.GenerateLinkAsync(creatorId, unitId, "Member", 24, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInviteDetailsAsync_ShouldReturnDetails()
    {
        var token = "token123";
        var link = new InviteLink 
        { 
            Token = token, 
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsActive = true,
            OrganizationUnit = new OrganizationUnit { Name = "Team" },
            Role = new Role { Name = "Member" }
        };

        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(link);

        var result = await _inviteLinkService.GetInviteDetailsAsync(token);

        result.Should().NotBeNull();
        result!.OrganizationUnitName.Should().Be("Team");
        result.RoleName.Should().Be("Member");
    }

    [Fact]
    public async Task JoinViaLinkAsync_ShouldThrowException_WhenTokenIsInvalid()
    {
        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((InviteLink?)null);
        var action = async () => await _inviteLinkService.JoinViaLinkAsync("invalid", Guid.NewGuid());
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid or non-existent invite link.");
    }

    [Fact]
    public async Task JoinViaLinkAsync_ShouldThrowException_WhenLinkIsExpired()
    {
        var link = new InviteLink { ExpiresAt = DateTime.UtcNow.AddMinutes(-5), IsActive = true };
        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(link);
        
        var action = async () => await _inviteLinkService.JoinViaLinkAsync("token", Guid.NewGuid());
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("This invite link has expired.");
    }

    [Fact]
    public async Task JoinViaLinkAsync_ShouldThrowException_WhenLinkIsInactive()
    {
        var link = new InviteLink { ExpiresAt = DateTime.UtcNow.AddMinutes(5), IsActive = false };
        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(link);
        
        var action = async () => await _inviteLinkService.JoinViaLinkAsync("token", Guid.NewGuid());
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("This invite link has expired.");
    }

    [Fact]
    public async Task JoinViaLinkAsync_ShouldReturnFalse_WhenMaxUsesReached()
    {
        var link = new InviteLink { ExpiresAt = DateTime.UtcNow.AddMinutes(5), IsActive = true, MaxUses = 1, CurrentUses = 1 };
        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(link);
        
        var action = async () => await _inviteLinkService.JoinViaLinkAsync("token", Guid.NewGuid());
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("This link has reached the maximum allowed uses.");
    }

    [Fact]
    public async Task JoinViaLinkAsync_ShouldAddUserToUnit_WhenValid()
    {
        var unitId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var link = new InviteLink 
        { 
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), 
            IsActive = true, 
            MaxUses = 2, 
            CurrentUses = 0,
            OrganizationUnitId = unitId,
            RoleId = roleId
        };
        
        _inviteLinkRepositoryMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(link);
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(userId, unitId)).ReturnsAsync((UserUnitRole?)null);
        _unitRepositoryMock.Setup(r => r.AddMemberAsync(It.IsAny<UserUnitRole>())).Returns(Task.CompletedTask);
        _inviteLinkRepositoryMock.Setup(r => r.UpdateAsync(link)).Returns(Task.CompletedTask);
        
        var result = await _inviteLinkService.JoinViaLinkAsync("token", userId);
        
        result.Should().BeTrue();
        link.CurrentUses.Should().Be(1);
        _unitRepositoryMock.Verify(r => r.AddMemberAsync(It.IsAny<UserUnitRole>()), Times.Once);
        _inviteLinkRepositoryMock.Verify(r => r.UpdateAsync(link), Times.Once);
    }
}

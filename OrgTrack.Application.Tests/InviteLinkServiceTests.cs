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
}

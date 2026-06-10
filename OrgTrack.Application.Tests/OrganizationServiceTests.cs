using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Application.Tests;

public class OrganizationServiceTests
{
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly ActivityLogService _activityLogService;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceTests()
    {
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object, new Mock<IUserRepository>().Object);

        _organizationService = new OrganizationService(
            _unitRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userRepositoryMock.Object,
            _activityLogService
        );
    }

    // CREATE UNIT
    [Fact]
    public async Task CreateUnitAsync_ShouldThrowException_WhenTypeIsInvalid()
    {
        var action = async () => await _organizationService.CreateUnitAsync(
            "Test Unit", "Desc", "InvalidType", null, null, Guid.NewGuid());
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateUnitAsync_ShouldCreateUnit_WhenValidData()
    {
        var parentId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId))
            .ReturnsAsync(new OrganizationUnit { Id = parentId, Type = UnitType.National });

        OrganizationUnit? capturedUnit = null;
        _unitRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<OrganizationUnit>()))
            .Callback<OrganizationUnit>(u => capturedUnit = u)
            .Returns(Task.CompletedTask);

        var result = await _organizationService.CreateUnitAsync(
            "Test Committee", "Desc", "Committee", null, parentId, creatorId);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test Committee");
        capturedUnit.Should().NotBeNull();
        capturedUnit!.Type.Should().Be(UnitType.Committee);
    }

    // GET UNIT
    [Fact]
    public async Task GetUnitByIdAsync_ShouldReturnUnit_WhenFound()
    {
        var unitId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = unitId, Name = "Test" };
        _unitRepositoryMock.Setup(repo => repo.GetByIdWithChildrenAsync(unitId))
            .ReturnsAsync(unit);

        var result = await _organizationService.GetUnitByIdAsync(unitId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(unitId);
    }

    // UPDATE UNIT
    [Fact]
    public async Task UpdateUnitAsync_ShouldReturnUpdatedUnit()
    {
        var unitId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = unitId, Name = "Old" };
        _unitRepositoryMock.Setup(repo => repo.GetByIdAsync(unitId))
            .ReturnsAsync(unit);
        _unitRepositoryMock.Setup(repo => repo.UpdateAsync(unit)).Returns(Task.CompletedTask);

        var result = await _organizationService.UpdateUnitAsync(unitId, "New Name", "New Desc");

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        unit.Name.Should().Be("New Name");
    }

    // DELETE UNIT
    [Fact]
    public async Task DeleteUnitAsync_ShouldThrow_WhenUnitHasChildren()
    {
        var unitId = Guid.NewGuid();
        var unit = new OrganizationUnit 
        { 
            Id = unitId, 
            ChildUnits = new List<OrganizationUnit> { new OrganizationUnit() } 
        };
        _unitRepositoryMock.Setup(repo => repo.GetByIdWithChildrenAsync(unitId))
            .ReturnsAsync(unit);

        var action = async () => await _organizationService.DeleteUnitAsync(unitId);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteUnitAsync_ShouldReturnTrue_WhenSuccess()
    {
        var unitId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = unitId };
        _unitRepositoryMock.Setup(repo => repo.GetByIdWithChildrenAsync(unitId))
            .ReturnsAsync(unit);
        _unitRepositoryMock.Setup(repo => repo.DeleteAsync(unit)).Returns(Task.CompletedTask);

        var result = await _organizationService.DeleteUnitAsync(unitId);

        result.Should().BeTrue();
    }

    // ASSIGN MEMBER
    [Fact]
    public async Task AssignMemberAsync_ShouldAssign_WhenValid()
    {
        var unitId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Member" };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(role.Name)).ReturnsAsync(role);
        
        var unit = new OrganizationUnit { Id = unitId };
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);
        UserUnitRole? capturedRole = null;
        _unitRepositoryMock.Setup(r => r.AddMemberAsync(It.IsAny<UserUnitRole>()))
            .Callback<UserUnitRole>(r => capturedRole = r)
            .Returns(Task.CompletedTask);

        var result = await _organizationService.AssignMemberAsync(unitId, user.Email, role.Name, adminId);

        result.Should().NotBeNull();
        capturedRole.Should().NotBeNull();
        capturedRole!.UserId.Should().Be(user.Id);
        capturedRole.RoleId.Should().Be(role.Id);
    }

    // REMOVE MEMBER
    [Fact]
    public async Task RemoveMemberAsync_ShouldReturnTrue_WhenMemberExists()
    {
        var unitId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var membership = new UserUnitRole { UserId = userId, OrganizationUnitId = unitId };

        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(userId, unitId)).ReturnsAsync(membership);
        _unitRepositoryMock.Setup(r => r.RemoveMemberAsync(membership)).Returns(Task.CompletedTask);

        var result = await _organizationService.RemoveMemberAsync(unitId, userId);

        result.Should().BeTrue();
    }

    // UPDATE MEMBER ROLE
    [Fact]
    public async Task UpdateMemberRoleAsync_ShouldThrow_WhenRankIsTooLow()
    {
        var unitId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        
        var actorUser = new User { Id = actorUserId, Email = "regular@aiesec.ro" };
        var actorRole = new Role { Name = "Member" }; // Rank 20
        var actorRoles = new List<UserUnitRole> { new UserUnitRole { Role = actorRole } };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(actorUserId)).ReturnsAsync(actorUser);
        _unitRepositoryMock.Setup(r => r.GetUserRolesAsync(actorUserId)).ReturnsAsync(actorRoles);

        var action = async () => await _organizationService.UpdateMemberRoleAsync(
            unitId, targetUserId, "LocalPresident", actorUserId); // Target Rank 80

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Security Violation: You do not have a high enough role to assign this level of permission.");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_ShouldUpdate_WhenAdmin()
    {
        var unitId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        
        var actorUser = new User { Id = actorUserId, Email = "admin@aiesec.ro" }; // Admin backdoor
        _userRepositoryMock.Setup(r => r.GetByIdAsync(actorUserId)).ReturnsAsync(actorUser);
        _unitRepositoryMock.Setup(r => r.GetUserRolesAsync(actorUserId)).ReturnsAsync(new List<UserUnitRole>());

        var targetRole = new Role { Id = Guid.NewGuid(), Name = "NationalPresident" };
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("NationalPresident")).ReturnsAsync(targetRole);

        var membership = new UserUnitRole { UserId = targetUserId, OrganizationUnitId = unitId };
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(targetUserId, unitId)).ReturnsAsync(membership);
        _unitRepositoryMock.Setup(r => r.UpdateMemberAsync(membership)).Returns(Task.CompletedTask);

        var result = await _organizationService.UpdateMemberRoleAsync(unitId, targetUserId, "NationalPresident", actorUserId);

        result.Should().BeTrue();
        membership.RoleId.Should().Be(targetRole.Id);
    }

    // GET FULL TREE
    [Fact]
    public async Task GetFullTreeAsync_ShouldBuildTree()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var root = new OrganizationUnit { Id = rootId, Name = "Romania", Type = UnitType.National, Members = new List<UserUnitRole>() };
        var child = new OrganizationUnit { Id = childId, ParentUnitId = rootId, Name = "Bucuresti", Type = UnitType.Committee, Members = new List<UserUnitRole>() };

        var allUnits = new List<OrganizationUnit> { root, child };
        _unitRepositoryMock.Setup(r => r.GetAllUnitsAsync()).ReturnsAsync(allUnits);

        var result = await _organizationService.GetFullTreeAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(1); // 1 root
        result[0].Children.Should().HaveCount(1); // 1 child
        result[0].Children[0].Name.Should().Be("Bucuresti");
    }

    // GET MY UNITS
    [Fact]
    public async Task GetMyUnitsAsync_ShouldReturnMappedUnits()
    {
        var userId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = Guid.NewGuid(), Name = "My Unit" };
        var roles = new List<UserUnitRole> 
        { 
            new UserUnitRole { OrganizationUnitId = unit.Id, OrganizationUnit = unit } 
        };

        _unitRepositoryMock.Setup(r => r.GetUserRolesAsync(userId)).ReturnsAsync(roles);

        var result = await _organizationService.GetMyUnitsAsync(userId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("My Unit");
    }
}

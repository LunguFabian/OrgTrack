using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Application.Tests;

public class OrganizationServiceExtendedTests
{
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly ActivityLogService _activityLogService;
    private readonly OrganizationService _sut;

    public OrganizationServiceExtendedTests()
    {
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object, new Mock<IUserRepository>().Object);

        _sut = new OrganizationService(
            _unitRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userRepositoryMock.Object,
            _activityLogService
        );
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldThrow_WhenRoleNotFound()
    {
        var unitId = Guid.NewGuid();
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit());
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("FakeRole")).ReturnsAsync((Role?)null);
        _roleRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role>());

        var action = async () => await _sut.AssignMemberAsync(unitId, "test@test.com", "FakeRole", Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("*does not exist*");
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldCreateUser_WhenUserNotFound()
    {
        var unitId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = "Member" };
        
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit());
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("Member")).ReturnsAsync(role);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        
        User? capturedUser = null;
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        _unitRepositoryMock.Setup(r => r.AddMemberAsync(It.IsAny<UserUnitRole>())).Returns(Task.CompletedTask);

        await _sut.AssignMemberAsync(unitId, "new@test.com", "Member", adminId);

        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldThrow_WhenUserAlreadyInUnit()
    {
        var unitId = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = "Member" };
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com" };

        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit());
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("Member")).ReturnsAsync(role);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _unitRepositoryMock.Setup(r => r.GetUserUnitRoleAsync(user.Id, unitId)).ReturnsAsync(new UserUnitRole());

        var action = async () => await _sut.AssignMemberAsync(unitId, "test@test.com", "Member", Guid.NewGuid());

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already has a role*");
    }

    [Fact]
    public async Task GetMembersAsync_ShouldReturnEmptyList_WhenUnitNull()
    {
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrganizationUnit?)null);

        var result = await _sut.GetMembersAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMembersAsync_ShouldGetDescendants_WhenNotNational()
    {
        var unitId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = unitId, Type = UnitType.Committee };
        var userId = Guid.NewGuid();

        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(unitId)).ReturnsAsync(new HashSet<Guid> { Guid.NewGuid() });
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<UserUnitRole> 
            { 
                new UserUnitRole { UserId = userId, User = new User { FirstName = "A", LastName = "B", Email = "a@b.c" }, Role = new Role { Name = "Member" } } 
            });

        var result = await _sut.GetMembersAsync(unitId);

        result.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task UpdateUnitAsync_ShouldReturnNull_WhenUnitNull()
    {
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrganizationUnit?)null);

        var result = await _sut.UpdateUnitAsync(Guid.NewGuid(), "Name", "Desc");

        result.Should().BeNull();
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrgTrack.Api.Models;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using OrgTrack.Infrastructure.Persistence;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class OrganizationControllerTests : IntegrationTestBase
{
    public OrganizationControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateUnit_ShouldReturnCreated_WhenUserHasPermissions()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid parentUnitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = parentUnitId, Name = "National", Type = UnitType.National };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Units.Manage\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = parentUnitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);
        var request = new CreateUnitRequest("Test Committee", "Desc", "Committee", null, parentUnitId);

        // Act
        var response = await Client.PostAsJsonAsync("/api/organization/units", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetUnit_ShouldReturnUnit_WhenUserHasViewPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Local Committee", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);

        // Act
        var response = await Client.GetAsync($"/api/organization/units/{unitId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUnit_ShouldReturnOk_WhenUserHasManagePermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Old Name", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Units.Manage\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);
        var request = new UpdateUnitRequest("New Name", "New Description");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/organization/units/{unitId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUnit_ShouldReturnNoContent_WhenUserHasManagePermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Unit To Delete", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Units.Manage\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);

        // Act
        var response = await Client.DeleteAsync($"/api/organization/units/{unitId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetTree_ShouldReturnTree_WhenUserHasViewPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid parentUnitId = Guid.NewGuid();
        Guid childUnitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var parent = new OrganizationUnit { Id = parentUnitId, Name = "Parent", Type = UnitType.National };
            var child = new OrganizationUnit { Id = childUnitId, Name = "Child", Type = UnitType.Committee, ParentUnitId = parentUnitId };
            db.OrganizationUnits.AddRange(parent, child);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = parentUnitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);

        // Act
        var response = await Client.GetAsync($"/api/organization/tree");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignMember_ShouldReturnOk_WhenUserHasManageMembersPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid targetUserId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();
        Guid memberRoleId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var manageRole = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Members.Manage\"]" };
            var memberRole = new Role { Id = memberRoleId, Name = "Member", Permissions = "[\"Units.View\"]" };
            db.Roles.AddRange(manageRole, memberRole);
            
            db.Users.Add(new User { Id = targetUserId, Email = "target@test.com", FirstName = "Target", LastName = "User" });
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = manageRole.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);
        var request = new AssignMemberRequest("target@test.com", "Member");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/organization/units/{unitId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMembers_ShouldReturnOk_WhenUserHasViewPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);

        // Act
        var response = await Client.GetAsync($"/api/organization/units/{unitId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateMemberRole_ShouldReturnOk_WhenUserHasManageMembersPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid targetUserId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();
        Guid newRoleId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var manageRole = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Members.Manage\"]" };
            var memberRole = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Units.View\"]" };
            var newRole = new Role { Id = newRoleId, Name = "TL", Permissions = "[\"Units.View\", \"Tasks.Manage\"]" };
            db.Roles.AddRange(manageRole, memberRole, newRole);
            
            db.Users.Add(new User { Id = targetUserId, Email = "target@test.com", FirstName = "Target", LastName = "User" });
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = manageRole.Id });
            db.UserUnitRoles.Add(new UserUnitRole { UserId = targetUserId, OrganizationUnitId = unitId, RoleId = memberRole.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);
        var request = new UpdateMemberRoleRequest("TL");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/organization/units/{unitId}/members/{targetUserId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturnNoContent_WhenUserHasManageMembersPermission()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid targetUserId = Guid.NewGuid();
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var manageRole = new Role { Id = Guid.NewGuid(), Name = "President", Permissions = "[\"Members.Manage\"]" };
            var memberRole = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Units.View\"]" };
            db.Roles.AddRange(manageRole, memberRole);
            
            db.Users.Add(new User { Id = targetUserId, Email = "target@test.com", FirstName = "Target", LastName = "User" });
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = manageRole.Id });
            db.UserUnitRoles.Add(new UserUnitRole { UserId = targetUserId, OrganizationUnitId = unitId, RoleId = memberRole.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);

        // Act
        var response = await Client.DeleteAsync($"/api/organization/units/{unitId}/members/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMyUnits_ShouldReturnUnits()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        AuthenticateAs(testUserId);
        
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == testUserId))
                db.Users.Add(new User { Id = testUserId, Email = "test@test.com", FirstName = "Test", LastName = "User" });
            
            var unit = new OrganizationUnit { Id = Guid.NewGuid(), Name = "Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[]" };
            if (!db.Roles.Any(r => r.Name == "Member")) db.Roles.Add(role);
            else role = db.Roles.First(r => r.Name == "Member");
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/me/units");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchMembers_ShouldReturnOk()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        AuthenticateAs(testUserId);
        
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == testUserId))
                db.Users.Add(new User { Id = testUserId, Email = "test@test.com", FirstName = "Test", LastName = "User" });
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/organization/search-members?q=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserProfile_ShouldReturnOk()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        AuthenticateAs(testUserId);
        
        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == testUserId))
                db.Users.Add(new User { Id = testUserId, Email = "test@test.com", FirstName = "Test", LastName = "User" });
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync($"/api/organization/users/{testUserId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

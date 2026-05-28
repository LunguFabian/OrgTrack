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
}

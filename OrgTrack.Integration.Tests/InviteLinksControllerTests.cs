using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Api.Models;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class InviteLinksControllerTests : IntegrationTestBase
{
    public InviteLinksControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GenerateLink_ShouldReturnOk_WhenUserHasPermissions()
    {
        // Arrange
        var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid unitId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Id = unitId, Name = "Team", Type = UnitType.Team };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Id = Guid.NewGuid(), Name = "Leader", Permissions = "[\"Members.Manage\"]" };
            db.Roles.Add(role);
            
            var targetRole = new Role { Id = Guid.NewGuid(), Name = "Member", Permissions = "[\"Members.View\"]" };
            db.Roles.Add(targetRole);
            
            db.UserUnitRoles.Add(new UserUnitRole { UserId = testUserId, OrganizationUnitId = unitId, RoleId = role.Id });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(testUserId);
        var request = new GenerateInviteLinkRequest(unitId, "Member", 24, 10);

        // Act
        var response = await Client.PostAsJsonAsync("/api/invite-links", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

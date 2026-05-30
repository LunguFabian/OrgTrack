using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Api.Models;
using OrgTrack.Application.DTOs;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class InviteLinksControllerTests : IntegrationTestBase
{
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public InviteLinksControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GenerateLink_ShouldReturnCreatedLink_WhenUserHasPermission()
    {
        AuthenticateAs(_testUserId);
        Guid unitId = Guid.Empty;

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Leader", LastName = "User", Email = "leader@test.com" });
            
            var unit = new OrganizationUnit { Name = "Committee", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);

            var manageRole = new Role { Name = "President", Permissions = "[\"Members.Manage\"]" };
            var memberRole = new Role { Name = "Member", Permissions = "[]" };
            db.Roles.AddRange(manageRole, memberRole);

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = manageRole.Id });

            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var request = new GenerateInviteLinkRequest(unitId, "Member", 24, 10);

        var response = await Client.PostAsJsonAsync("/api/invite-links", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var linkDto = await response.Content.ReadFromJsonAsync<InviteLinkCreatedDto>();
        linkDto.Should().NotBeNull();
        linkDto!.Token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetInviteDetails_ShouldReturnPreview_WhenLinkExists()
    {
        string token = "test-token-123";
        Guid unitId = Guid.Empty;
        Guid creatorUserId = Guid.NewGuid();

        await ExecuteInDbAsync(async db =>
        {
            db.Users.Add(new User { Id = creatorUserId, FirstName = "Creator", LastName = "User", Email = "creator@test.com" });
            
            var unit = new OrganizationUnit { Name = "Committee", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Member", Permissions = "[]" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            unitId = unit.Id;

            db.InviteLinks.Add(new InviteLink
            {
                Id = Guid.NewGuid(),
                Token = token,
                OrganizationUnitId = unitId,
                RoleId = role.Id,
                CreatedByUserId = creatorUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                MaxUses = 10,
                CurrentUses = 0,
                IsActive = true
            });

            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync($"/api/invite-links/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<InviteLinkPreviewDto>();
        preview.Should().NotBeNull();
        preview!.OrganizationUnitName.Should().Be("Committee");
    }

    [Fact]
    public async Task JoinViaLink_ShouldReturnOk_WhenLinkIsValid()
    {
        AuthenticateAs(_testUserId);
        string token = "join-test-token";

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "New", LastName = "Member", Email = "new@test.com" });
            
            var unit = new OrganizationUnit { Name = "Committee", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Member", Permissions = "[]" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            db.InviteLinks.Add(new InviteLink
            {
                Id = Guid.NewGuid(),
                Token = token,
                OrganizationUnitId = unit.Id,
                RoleId = role.Id,
                CreatedByUserId = _testUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                MaxUses = 10,
                CurrentUses = 0,
                IsActive = true
            });

            await db.SaveChangesAsync();
        });

        var response = await Client.PostAsJsonAsync($"/api/invite-links/{token}/join", new {});

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

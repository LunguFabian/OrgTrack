using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class AnalyticsControllerTests : IntegrationTestBase
{
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public AnalyticsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMemberScore_ShouldReturnScore_WhenUserHasAccess()
    {
        AuthenticateAs(_testUserId);
        
        Guid unitId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Local Committee", Description = "Desc", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Leader", Permissions = "[\"Members.View\"]" };
            db.Roles.Add(role);

            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;

            var task1 = new TaskItem 
            { 
                Title = "Task 1", 
                Description = "Desc", 
                Priority = TaskPriority.High, 
                OrganizationUnitId = unitId, 
                AssigneeId = _testUserId, 
                CreatorId = _testUserId,
                Status = OrgTrack.Domain.Enums.TaskStatus.Done
            };
            db.Tasks.Add(task1);

            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync($"/api/analytics/members/{_testUserId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnitSummary_ShouldReturnSummary_WhenUserHasAccess()
    {
        AuthenticateAs(_testUserId);
        
        Guid unitId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Local Committee", Description = "Desc", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Leader", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);

            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var response = await Client.GetAsync($"/api/analytics/units/{unitId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNationalDashboard_ShouldReturnDashboards_WhenUserHasAccess()
    {
        AuthenticateAs(_testUserId);
        
        Guid unitId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "National", Description = "Desc", Type = UnitType.National };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "President", Permissions = "[\"Units.Manage\"]" };
            db.Roles.Add(role);

            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var response = await Client.GetAsync($"/api/analytics/national/{unitId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnLeaderboard_WhenUserHasAccess()
    {
        AuthenticateAs(_testUserId);
        
        Guid unitId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Local Committee", Description = "Desc", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Leader", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);

            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var response = await Client.GetAsync($"/api/analytics/units/{unitId}/leaderboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReport_ShouldReturnPdf_WhenUserHasAccess()
    {
        AuthenticateAs(_testUserId);
        
        Guid unitId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Local Committee", Description = "Desc", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            
            var role = new Role { Name = "Leader", Permissions = "[\"Units.View\"]" };
            db.Roles.Add(role);

            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });

            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var response = await Client.GetAsync($"/api/analytics/units/{unitId}/report?format=pdf");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetHierarchicalBurnoutRisks_ShouldReturnRisks()
    {
        AuthenticateAs(_testUserId);

        await ExecuteInDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.Id == _testUserId)) 
                db.Users.Add(new User { Id = _testUserId, FirstName = "Test", LastName = "User", Email = "test@test.com" });
            await db.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/api/analytics/burnout-risks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

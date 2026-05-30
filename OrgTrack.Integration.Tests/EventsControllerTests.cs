using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrgTrack.Api.Models;
using OrgTrack.Application.DTOs;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using OrgTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrgTrack.Integration.Tests;

public class EventsControllerTests : IntegrationTestBase
{
    public EventsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyEvents_ShouldReturnSuccessAndEmptyList_WhenNoEventsExist()
    {
        var response = await Client.GetAsync("/api/me/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().NotBeNull();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnForbidden_WhenUserDoesNotHavePermissions()
    {
        // Act
        var unitId = Guid.NewGuid();
        var createEventReq = new CreateEventRequest(
            "Integration Test Event", "This is a test", DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2), false, null, null, new List<Guid>(), new List<Guid>()
        );

        var response = await Client.PostAsJsonAsync($"/api/organization/units/{unitId}/events", createEventReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnCreated_WhenUserHasPermissions()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Events.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            unitId = unit.Id;
        });

        var createEventReq = new CreateEventRequest(
            "Test Event", "Description", DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2), false, null, null, new List<Guid>(), new List<Guid>()
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/organization/units/{unitId}/events", createEventReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        createdEvent!.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task Rsvp_ShouldReturnOk_WhenUserIsMember()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid eventId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Member", Permissions = "[]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var ev = new Event { Title = "Test Event", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1), OrganizationUnitId = unit.Id };
            db.Events.Add(ev);
            
            await db.SaveChangesAsync();
            unitId = unit.Id;
            eventId = ev.Id;
        });

        var rsvpReq = new RsvpRequest(PresenceStatus.Present);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/organization/units/{unitId}/events/{eventId}/rsvp", rsvpReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEvent_ShouldReturnOk_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid eventId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Events.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var ev = new Event { Title = "Test Event", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1), OrganizationUnitId = unit.Id };
            db.Events.Add(ev);
            
            await db.SaveChangesAsync();
            unitId = unit.Id;
            eventId = ev.Id;
        });

        var updateEventReq = new UpdateEventRequest(
            "New Title", "Description", DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2), false, null, null, new List<Guid>(), new List<Guid>()
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/organization/units/{unitId}/events/{eventId}", updateEventReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteEvent_ShouldReturnNoContent_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid eventId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Events.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var ev = new Event { Title = "Test Event", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1), OrganizationUnitId = unit.Id };
            db.Events.Add(ev);
            
            await db.SaveChangesAsync();
            unitId = unit.Id;
            eventId = ev.Id;
        });

        // Act
        var response = await Client.DeleteAsync($"/api/organization/units/{unitId}/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAttendance_ShouldReturnOk_WhenUserHasManagePermission()
    {
        // Arrange
        Guid unitId = Guid.Empty;
        var _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid eventId = Guid.Empty;
        await ExecuteInDbAsync(async db =>
        {
            var unit = new OrganizationUnit { Name = "Test Unit", Type = UnitType.Committee };
            db.OrganizationUnits.Add(unit);
            var role = new Role { Name = "Leader", Permissions = "[\"Events.Manage\"]" };
            db.Roles.Add(role);
            db.UserUnitRoles.Add(new UserUnitRole { UserId = _testUserId, OrganizationUnitId = unit.Id, RoleId = role.Id });
            
            var ev = new Event { Title = "Test Event", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1), OrganizationUnitId = unit.Id };
            db.Events.Add(ev);
            
            await db.SaveChangesAsync();
            unitId = unit.Id;
            eventId = ev.Id;
        });

        // Act
        var response = await Client.GetAsync($"/api/organization/units/{unitId}/events/{eventId}/attendance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

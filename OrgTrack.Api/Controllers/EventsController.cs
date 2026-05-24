using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Api.Models;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Constants;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/organization/units/{unitId:guid}/events")]
public class EventsController(
    EventService eventService,
    IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEvents(Guid unitId)
    {
        var userId = User.GetUserId();

        bool hasPermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.EventsView)
            || await permissionService.HasPermissionAsync(userId, unitId, Permissions.EventsManage);
        bool isDirectMember = await permissionService.IsDirectMemberAsync(userId, unitId);

        if (!hasPermission && !isDirectMember)
        {
            return Forbid();
        }

        var events = await eventService.GetEventsByUnitAsync(unitId, userId);
        return Ok(events);
    }

    [HttpGet("/api/me/events")]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = User.GetUserId();
        var events = await eventService.GetMyEventsAsync(userId);
        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(Guid unitId, [FromBody] CreateEventRequest request)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.EventsManage))
        {
            return Forbid();
        }

        try
        {
            var ev = await eventService.CreateEventAsync(
                unitId,
                request.Title,
                request.Description,
                request.StartDate,
                request.EndDate,
                request.IsRecurring,
                request.RecurrencePattern,
                request.ExternalCalendarId,
                userId,
                request.InvitedUnitIds,
                request.InvitedUserIds
            );

            return CreatedAtAction(nameof(GetEvents), new { unitId }, ev);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{eventId:guid}/rsvp")]
    public async Task<IActionResult> Rsvp(Guid unitId, Guid eventId, [FromBody] RsvpRequest request)
    {
        var userId = User.GetUserId();
        bool canRsvp = await permissionService.HasPermissionAsync(userId, unitId, Permissions.EventsView)
            || await permissionService.IsDirectMemberAsync(userId, unitId);
        if (!canRsvp)
        {
            return Forbid();
        }

        try
        {
            await eventService.RsvpAsync(eventId, userId, request.Status);
            return Ok(new { message = "Your RSVP has been recorded." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{eventId:guid}/attendance/{targetUserId:guid}")]
    public async Task<IActionResult> ConfirmAttendance(Guid unitId, Guid eventId, Guid targetUserId, [FromBody] RsvpRequest request)
    {
        var currentUserId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(currentUserId, unitId, Permissions.EventsManage))
        {
            return Forbid();
        }

        try
        {
            await eventService.RsvpAsync(eventId, targetUserId, request.Status);
            return Ok(new { message = "Attendance updated by leader." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{eventId:guid}/attendance")]
    public async Task<IActionResult> GetAttendanceReport(Guid unitId, Guid eventId)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.EventsManage))
        {
            return Forbid();
        }

        try
        {
            var report = await eventService.GetAttendanceReportAsync(eventId);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

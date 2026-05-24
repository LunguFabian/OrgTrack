using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.UseCases;

public class EventService(
    IEventRepository eventRepository,
    IOrganizationUnitRepository unitRepository,
    IUserRepository userRepository,
    ActivityLogService activityLogService)
{
    public async Task<EventDto> CreateEventAsync(
        Guid unitId, string title, string description, DateTime startDate, DateTime endDate,
        bool isRecurring, string? recurrencePattern, string? externalCalendarId, Guid creatorId, 
        List<Guid>? invitedUnitIds = null, List<Guid>? invitedUserIds = null)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) throw new ArgumentException("Organization unit not found.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after the start date.");

        var newEvent = new Event
        {
            Title = title,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            OrganizationUnitId = unitId,
            IsRecurring = isRecurring,
            RecurrencePattern = recurrencePattern,
            ExternalCalendarId = externalCalendarId
        };
        var targetUnits = invitedUnitIds != null && invitedUnitIds.Any() ? invitedUnitIds : new List<Guid> { unitId };
        foreach (var uId in targetUnits)
        {
            newEvent.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = uId });
        }
        if (invitedUserIds != null)
        {
            foreach (var uId in invitedUserIds)
            {
                newEvent.InvitedUsers.Add(new EventInvitedUser { UserId = uId });
            }
        }

        await eventRepository.AddAsync(newEvent);
        await activityLogService.LogEventCreatedAsync(creatorId, newEvent.Id, newEvent.Title, unitId);

        return MapToDto(newEvent);
    }

    public async Task<EventDto> UpdateEventAsync(
        Guid eventId, string title, string description, DateTime startDate, DateTime endDate,
        bool isRecurring, string? recurrencePattern, string? externalCalendarId,
        List<Guid>? invitedUnitIds = null, List<Guid>? invitedUserIds = null)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after the start date.");

        ev.Title = title;
        ev.Description = description;
        ev.StartDate = startDate;
        ev.EndDate = endDate;
        ev.IsRecurring = isRecurring;
        ev.RecurrencePattern = recurrencePattern;
        ev.ExternalCalendarId = externalCalendarId;
        ev.UpdatedAt = DateTime.UtcNow;

        ev.InvitedUnits.Clear();
        var targetUnits = invitedUnitIds != null && invitedUnitIds.Any() ? invitedUnitIds : new List<Guid> { ev.OrganizationUnitId };
        foreach (var uId in targetUnits)
        {
            ev.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = uId, EventId = ev.Id });
        }

        ev.InvitedUsers.Clear();
        if (invitedUserIds != null)
        {
            foreach (var uId in invitedUserIds)
            {
                ev.InvitedUsers.Add(new EventInvitedUser { UserId = uId, EventId = ev.Id });
            }
        }

        await eventRepository.UpdateAsync(ev);
        return MapToDto(ev);
    }

    public async Task DeleteEventAsync(Guid eventId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");

        await eventRepository.DeleteAsync(ev);
    }

    public async Task<IEnumerable<EventDto>> GetEventsByUnitAsync(Guid unitId, Guid userId)
    {
        var events = await eventRepository.GetByUnitIdAsync(unitId);
        var rsvps = await eventRepository.GetUserRsvpsAsync(userId, events.Select(e => e.Id));
        return events.Select(e => MapToDto(e, rsvps.FirstOrDefault(r => r.EventId == e.Id)?.Status.ToString()));
    }

    public async Task<IEnumerable<EventDto>> GetMyEventsAsync(Guid userId)
    {
        var roles = await unitRepository.GetUserRolesAsync(userId);
        var visibleUnitIds = new HashSet<Guid>();
        
        foreach (var role in roles)
        {
            visibleUnitIds.Add(role.OrganizationUnitId);
            var ancestors = await unitRepository.GetAncestorUnitIdsAsync(role.OrganizationUnitId);
            foreach (var a in ancestors) visibleUnitIds.Add(a);
        }

        var events = await eventRepository.GetVisibleEventsAsync(userId, visibleUnitIds);
        var rsvps = await eventRepository.GetUserRsvpsAsync(userId, events.Select(e => e.Id));
        return events.Select(e => MapToDto(e, rsvps.FirstOrDefault(r => r.EventId == e.Id)?.Status.ToString()));
    }

    public async Task RsvpAsync(Guid eventId, Guid userId, PresenceStatus status)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");
        var existingRsvp = await eventRepository.GetRsvpAsync(eventId, userId);

        if (existingRsvp != null)
        {
            existingRsvp.Status = status;
            existingRsvp.UpdatedAt = DateTime.UtcNow;
            await eventRepository.UpdateRsvpAsync(existingRsvp);
        }
        else
        {
            var rsvp = new EventRsvp
            {
                EventId = eventId,
                UserId = userId,
                Status = status
            };
            await eventRepository.AddRsvpAsync(rsvp);
        }
        await activityLogService.LogAttendanceConfirmedAsync(userId, userId, eventId, status.ToString(), ev.OrganizationUnitId);
    }

    public async Task<IEnumerable<AttendanceReportItemDto>> GetAttendanceReportAsync(Guid eventId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");
        var allRelevantUnitIds = new HashSet<Guid>();
        foreach (var iu in ev.InvitedUnits)
        {
            allRelevantUnitIds.Add(iu.OrganizationUnitId);
            var descendants = await unitRepository.GetDescendantUnitIdsAsync(iu.OrganizationUnitId);
            foreach (var d in descendants) allRelevantUnitIds.Add(d);
        }
        var members = await unitRepository.GetMembersForUnitsAsync(allRelevantUnitIds);
        
        var eligibleUsers = members
            .Select(m => m.User)
            .DistinctBy(u => u!.Id)
            .Where(u => u != null)
            .ToList();
        var explicitlyInvitedUserIds = ev.InvitedUsers.Select(iu => iu.UserId).ToList();
        var allEligibleUserIds = eligibleUsers.Select(u => u!.Id).ToList();
        
        var usersToFetch = explicitlyInvitedUserIds.Except(allEligibleUserIds).ToList();
        foreach (var uId in usersToFetch)
        {
            var user = await userRepository.GetByIdAsync(uId);
            if (user != null)
            {
                eligibleUsers.Add(user);
            }
        }
        var rsvps = await eventRepository.GetAttendanceReportAsync(eventId);
        var rsvpDict = rsvps.ToDictionary(r => r.UserId, r => r.Status);
        return eligibleUsers.Select(u => new AttendanceReportItemDto(
            u!.Id,
            $"{u.FirstName} {u.LastName}".Trim(),
            rsvpDict.TryGetValue(u.Id, out var status) ? status.ToString() : "NotResponded"
        ));
    }

    private static EventDto MapToDto(Event ev, string? currentUserRsvp = null)
    {
        return new EventDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartDate,
            ev.EndDate,
            ev.OrganizationUnitId,
            ev.InvitedUnits?.Select(u => u.OrganizationUnitId).ToList() ?? new List<Guid>(),
            ev.InvitedUsers?.Select(u => u.UserId).ToList() ?? new List<Guid>(),
            ev.IsRecurring,
            ev.RecurrencePattern,
            currentUserRsvp
        );
    }
}

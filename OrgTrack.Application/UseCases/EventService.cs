using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.UseCases;

public class EventService(
    IEventRepository eventRepository,
    IOrganizationUnitRepository unitRepository,
    IUserRepository userRepository,
    ActivityLogService activityLogService,
    IGoogleCalendarService googleCalendarService,
    NotificationService notificationService)
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
        var targetUnits = invitedUnitIds ?? new List<Guid>();
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

        var creator = await userRepository.GetByIdAsync(creatorId);
        if (creator != null && creator.IsGoogleCalendarConnected && !string.IsNullOrEmpty(creator.GoogleCalendarAccessToken))
        {
            var eligibleUsers = await GetEligibleUsersForEventAsync(newEvent);
            var attendeeEmails = eligibleUsers.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

            var googleEventId = await googleCalendarService.CreateEventAsync(
                creator.GoogleCalendarAccessToken,
                creator.GoogleCalendarRefreshToken ?? "",
                new EventCalendarData
                {
                    Title = newEvent.Title,
                    Description = newEvent.Description,
                    StartDate = newEvent.StartDate,
                    EndDate = newEvent.EndDate,
                    IsRecurring = newEvent.IsRecurring,
                    RecurrencePattern = newEvent.RecurrencePattern,
                    AttendeeEmails = attendeeEmails
                }
            );

            if (!string.IsNullOrEmpty(googleEventId))
            {
                newEvent.ExternalCalendarId = googleEventId;
                await eventRepository.UpdateAsync(newEvent);
            }
        }
        // Notify invited users about the new event
        var invitedUsers = await GetEligibleUsersForEventAsync(newEvent);
        foreach (var user in invitedUsers)
        {
            if (user.Id != creatorId)
            {
                await notificationService.CreateAndSendAsync(
                    user.Id, "EventCreated", "New Event",
                    $"You have been invited to \"{title}\"",
                    newEvent.Id, "Event", creatorId);
            }
        }

        return MapToDto(newEvent);
    }

    public async Task<EventDto> UpdateEventAsync(
        Guid userId, Guid eventId, string title, string description, DateTime startDate, DateTime endDate,
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
        var targetUnits = invitedUnitIds ?? new List<Guid>();
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

        if (!string.IsNullOrEmpty(ev.ExternalCalendarId))
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user != null && user.IsGoogleCalendarConnected && !string.IsNullOrEmpty(user.GoogleCalendarAccessToken))
            {
                var eligibleUsers = await GetEligibleUsersForEventAsync(ev);
                var attendeeEmails = eligibleUsers.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

                await googleCalendarService.UpdateEventAsync(
                    user.GoogleCalendarAccessToken,
                    user.GoogleCalendarRefreshToken ?? "",
                    ev.ExternalCalendarId,
                    new EventCalendarData
                    {
                        Title = ev.Title,
                        Description = ev.Description,
                        StartDate = ev.StartDate,
                        EndDate = ev.EndDate,
                        IsRecurring = ev.IsRecurring,
                        RecurrencePattern = ev.RecurrencePattern,
                        AttendeeEmails = attendeeEmails
                    }
                );
            }
        }

        // Notify invited users about the event edit
        var invitedUsers = await GetEligibleUsersForEventAsync(ev);
        foreach (var user in invitedUsers)
        {
            if (user.Id != userId) // userId is the person making the edit
            {
                await notificationService.CreateAndSendAsync(
                    user.Id, "EventUpdated", "Event Updated",
                    $"The event \"{title}\" has been modified.",
                    ev.Id, "Event", userId);
            }
        }

        return MapToDto(ev);
    }

    public async Task DeleteEventAsync(Guid userId, Guid eventId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");

        if (!string.IsNullOrEmpty(ev.ExternalCalendarId))
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user != null && user.IsGoogleCalendarConnected && !string.IsNullOrEmpty(user.GoogleCalendarAccessToken))
            {
                await googleCalendarService.DeleteEventAsync(
                    user.GoogleCalendarAccessToken,
                    user.GoogleCalendarRefreshToken ?? "",
                    ev.ExternalCalendarId
                );
            }
        }

        await eventRepository.DeleteAsync(ev);
    }

    public async Task<IEnumerable<EventDto>> GetEventsByUnitAsync(Guid unitId, Guid userId)
    {
        var events = await eventRepository.GetByUnitIdAsync(unitId);
        var rsvps = await eventRepository.GetUserRsvpsAsync(userId, events.Select(e => e.Id));
        return events.Select(e => MapToDto(e, rsvps.FirstOrDefault(r => r.EventId == e.Id)?.Rsvp.ToString()));
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
        return events.Select(e => MapToDto(e, rsvps.FirstOrDefault(r => r.EventId == e.Id)?.Rsvp.ToString()));
    }

    public async Task SetRsvpAsync(Guid eventId, Guid userId, RsvpStatus rsvp)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");
        var existingRsvp = await eventRepository.GetRsvpAsync(eventId, userId);

        if (existingRsvp != null)
        {
            existingRsvp.Rsvp = rsvp;
            existingRsvp.UpdatedAt = DateTime.UtcNow;
            await eventRepository.UpdateRsvpAsync(existingRsvp);
        }
        else
        {
            var newRsvp = new EventRsvp
            {
                EventId = eventId,
                UserId = userId,
                Rsvp = rsvp
            };
            await eventRepository.AddRsvpAsync(newRsvp);
        }
        await activityLogService.LogAttendanceConfirmedAsync(userId, userId, eventId, rsvp.ToString(), ev.OrganizationUnitId);
    }

    public async Task SetAttendanceAsync(Guid eventId, Guid targetUserId, AttendanceStatus attendance)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");
        var existingRsvp = await eventRepository.GetRsvpAsync(eventId, targetUserId);

        if (existingRsvp != null)
        {
            existingRsvp.Attendance = attendance;
            existingRsvp.UpdatedAt = DateTime.UtcNow;
            await eventRepository.UpdateRsvpAsync(existingRsvp);
        }
        else
        {
            var newRsvp = new EventRsvp
            {
                EventId = eventId,
                UserId = targetUserId,
                Attendance = attendance
            };
            await eventRepository.AddRsvpAsync(newRsvp);
        }
        await activityLogService.LogAttendanceConfirmedAsync(targetUserId, targetUserId, eventId, attendance.ToString(), ev.OrganizationUnitId);
    }

    private async Task<List<User>> GetEligibleUsersForEventAsync(Guid eventId)
    {
        var ev = await eventRepository.GetByIdAsync(eventId);
        if (ev == null) throw new ArgumentException("Event not found.");
        return await GetEligibleUsersForEventAsync(ev);
    }

    private async Task<List<User>> GetEligibleUsersForEventAsync(Event ev)
    {
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
        return eligibleUsers!;
    }

    public async Task<bool> IsUserEligibleForEventAsync(Guid eventId, Guid userId)
    {
        var eligibleUsers = await GetEligibleUsersForEventAsync(eventId);
        return eligibleUsers.Any(u => u.Id == userId);
    }

    public async Task<IEnumerable<AttendanceReportItemDto>> GetAttendanceReportAsync(Guid eventId)
    {
        var eligibleUsers = await GetEligibleUsersForEventAsync(eventId);
        var rsvps = await eventRepository.GetAttendanceReportAsync(eventId);
        var rsvpDict = rsvps.ToDictionary(r => r.UserId, r => r);
        return eligibleUsers.Select(u =>
        {
            var hasRsvp = rsvpDict.TryGetValue(u!.Id, out var rsvp);
            return new AttendanceReportItemDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                hasRsvp ? rsvp!.Rsvp.ToString() : RsvpStatus.NoResponse.ToString(),
                hasRsvp ? rsvp!.Attendance.ToString() : AttendanceStatus.Unmarked.ToString()
            );
        });
    }

    public async Task<IEnumerable<RsvpSummaryItemDto>> GetRsvpSummaryAsync(Guid eventId)
    {
        var eligibleUsers = await GetEligibleUsersForEventAsync(eventId);
        var rsvps = await eventRepository.GetAttendanceReportAsync(eventId);
        var rsvpDict = rsvps.ToDictionary(r => r.UserId, r => r.Rsvp);
        return eligibleUsers.Select(u => new RsvpSummaryItemDto(
            u!.Id,
            $"{u.FirstName} {u.LastName}".Trim(),
            rsvpDict.TryGetValue(u.Id, out var rsvpStatus) ? rsvpStatus.ToString() : RsvpStatus.NoResponse.ToString()
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
            ev.ExternalCalendarId,
            currentUserRsvp
        );
    }
}

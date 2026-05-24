using OrgTrack.Domain.Enums;

namespace OrgTrack.Api.Models;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsRecurring,
    string? RecurrencePattern,
    string? ExternalCalendarId,
    List<Guid> InvitedUnitIds,
    List<Guid> InvitedUserIds
);

public record RsvpRequest(
    PresenceStatus Status
);

public record UpdateEventRequest(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsRecurring,
    string? RecurrencePattern,
    string? ExternalCalendarId,
    List<Guid> InvitedUnitIds,
    List<Guid> InvitedUserIds
);

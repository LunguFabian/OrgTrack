using System.Text.Json.Serialization;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Api.Models;

public record CreateEventRequest(
    string Title,
    string Description,
    [property: JsonRequired] DateTime StartDate,
    [property: JsonRequired] DateTime EndDate,
    [property: JsonRequired] bool IsRecurring,
    string? RecurrencePattern,
    string? ExternalCalendarId,
    List<Guid> InvitedUnitIds,
    List<Guid> InvitedUserIds
);

public record RsvpRequest(
    [property: JsonRequired] PresenceStatus Status
);

public record UpdateEventRequest(
    string Title,
    string Description,
    [property: JsonRequired] DateTime StartDate,
    [property: JsonRequired] DateTime EndDate,
    [property: JsonRequired] bool IsRecurring,
    string? RecurrencePattern,
    string? ExternalCalendarId,
    List<Guid> InvitedUnitIds,
    List<Guid> InvitedUserIds
);

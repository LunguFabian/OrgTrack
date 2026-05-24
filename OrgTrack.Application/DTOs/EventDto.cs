using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.DTOs;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    Guid OrganizationUnitId,
    List<Guid> InvitedUnitIds,
    List<Guid> InvitedUserIds,
    bool IsRecurring,
    string? RecurrencePattern,
    string? CurrentUserRsvp = null
);

public record AttendanceReportItemDto(
    Guid UserId,
    string UserName,
    string Status
);

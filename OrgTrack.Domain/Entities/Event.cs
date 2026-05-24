using OrgTrack.Domain.Enums;

namespace OrgTrack.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public string? ExternalCalendarId { get; set; } 

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public ICollection<EventRsvp> Rsvps { get; set; } = new List<EventRsvp>();
    public ICollection<EventInvitedUnit> InvitedUnits { get; set; } = new List<EventInvitedUnit>();
    public ICollection<EventInvitedUser> InvitedUsers { get; set; } = new List<EventInvitedUser>();
}

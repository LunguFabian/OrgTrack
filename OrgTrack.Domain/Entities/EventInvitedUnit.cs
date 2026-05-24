namespace OrgTrack.Domain.Entities;

public class EventInvitedUnit
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
}

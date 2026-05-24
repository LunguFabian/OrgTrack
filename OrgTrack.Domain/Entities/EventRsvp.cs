namespace OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

public class EventRsvp : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public PresenceStatus Status { get; set; } = PresenceStatus.Unknown;
}

namespace OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

public class EventRsvp : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Member's self-declared intention BEFORE the event (Going / Maybe / NotGoing).
    /// </summary>
    public RsvpStatus Rsvp { get; set; } = RsvpStatus.NoResponse;

    /// <summary>
    /// Leader-confirmed attendance AFTER the event (Present / Absent / Excused).
    /// </summary>
    public AttendanceStatus Attendance { get; set; } = AttendanceStatus.Unmarked;
}

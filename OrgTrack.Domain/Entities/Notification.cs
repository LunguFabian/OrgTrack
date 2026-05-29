namespace OrgTrack.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Type { get; set; } = string.Empty; // TaskAssigned, TaskStatusChanged, EventCreated, EventUpdated, RsvpReceived
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "Task", "Event"
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }
}

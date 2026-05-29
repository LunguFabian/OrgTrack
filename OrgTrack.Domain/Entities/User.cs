namespace OrgTrack.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? GoogleCalendarAccessToken { get; set; }
    public string? GoogleCalendarRefreshToken { get; set; }
    public bool IsGoogleCalendarConnected { get; set; } = false;

    public ICollection<UserUnitRole> UnitRoles { get; set; } = new List<UserUnitRole>();
    
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<EventRsvp> EventRsvps { get; set; } = new List<EventRsvp>();
}
namespace OrgTrack.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<UserUnitRole> UnitRoles { get; set; } = new List<UserUnitRole>();
    
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<EventRsvp> EventRsvps { get; set; } = new List<EventRsvp>();
}
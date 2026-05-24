namespace OrgTrack.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty; 

    public ICollection<UserUnitRole> UserUnitRoles { get; set; } = new List<UserUnitRole>();
}

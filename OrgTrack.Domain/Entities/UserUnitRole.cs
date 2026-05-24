namespace OrgTrack.Domain.Entities;

public class UserUnitRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}

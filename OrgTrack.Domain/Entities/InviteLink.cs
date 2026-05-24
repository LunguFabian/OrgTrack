namespace OrgTrack.Domain.Entities;

public class InviteLink : BaseEntity
{
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int CurrentUses { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

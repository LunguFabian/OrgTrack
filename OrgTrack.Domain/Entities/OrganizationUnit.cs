namespace OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

public class OrganizationUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public UnitType Type { get; set; }
    public DepartmentType DepartmentType { get; set; } = DepartmentType.None;

    public Guid? ParentUnitId { get; set; }
    public OrganizationUnit? ParentUnit { get; set; }
    
    public ICollection<OrganizationUnit> ChildUnits { get; set; } = new List<OrganizationUnit>();
    
    public ICollection<UserUnitRole> Members { get; set; } = new List<UserUnitRole>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

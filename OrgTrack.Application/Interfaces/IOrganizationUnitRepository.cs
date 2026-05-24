using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.Interfaces;

public interface IOrganizationUnitRepository
{
    Task<OrganizationUnit?> GetByIdWithChildrenAsync(Guid id);
    Task<OrganizationUnit?> GetByIdAsync(Guid id);
    Task<HashSet<Guid>> GetAncestorUnitIdsAsync(Guid startUnitId);
    Task<HashSet<Guid>> GetDescendantUnitIdsAsync(Guid startUnitId);
    Task<List<OrganizationUnit>> GetRootUnitsAsync();
    Task<List<OrganizationUnit>> GetAllUnitsAsync();
    Task<List<OrganizationUnit>> GetByDepartmentTypeAsync(DepartmentType departmentType);
    
    Task AddAsync(OrganizationUnit unit);
    Task UpdateAsync(OrganizationUnit unit);
    Task DeleteAsync(OrganizationUnit unit);
    Task<List<UserUnitRole>> GetMembersAsync(Guid unitId);
    Task<List<UserUnitRole>> GetMembersForUnitsAsync(IEnumerable<Guid> unitIds);
    Task AddMemberAsync(UserUnitRole userUnitRole);
    Task UpdateMemberAsync(UserUnitRole userUnitRole);
    Task RemoveMemberAsync(UserUnitRole userUnitRole);
    Task<UserUnitRole?> GetUserUnitRoleAsync(Guid userId, Guid unitId);
    Task<List<UserUnitRole>> GetUserRolesAsync(Guid userId);
}

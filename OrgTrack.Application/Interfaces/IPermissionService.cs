namespace OrgTrack.Application.Interfaces;

public interface IPermissionService
{
    /// <summary>
    /// Checks if the user has the required permission on a specific organizational unit.
    /// Takes hierarchy into account: a role on a parent unit automatically grants permissions on child units.
    /// Ex: HasPermissionAsync(userId, teamId, "Members.Manage") will return true for the Team Leader, 
    /// the Department VP, the Local Committee President, and the National President.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid targetUnitId, string requiredPermission);

    /// <summary>
    /// Returns true if the user has any role directly in the given unit (regardless of permission level).
    /// Used to allow Members to view their own team page.
    /// </summary>
    Task<bool> IsDirectMemberAsync(Guid userId, Guid unitId);
}

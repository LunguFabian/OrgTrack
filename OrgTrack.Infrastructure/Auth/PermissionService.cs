using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Infrastructure.Persistence;

namespace OrgTrack.Infrastructure.Auth;

public class PermissionService(OrgTrackDbContext context) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(Guid userId, Guid targetUnitId, string requiredPermission)
    {
        if (await IsSuperAdminAsync(userId)) return true;

        var pathUnitIds = await GetPathToRootAsync(targetUnitId);
        var userRoles = await context.UserUnitRoles
            .Include(uur => uur.Role)
            .Where(uur => uur.UserId == userId && pathUnitIds.Contains(uur.OrganizationUnitId))
            .ToListAsync();

        return userRoles.Any(ur => ur.Role != null && RoleHasPermission(ur.Role.Permissions, requiredPermission));
    }

    private async Task<bool> IsSuperAdminAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user != null 
            && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
            && user.Email == "admin@aiesec.ro";
    }

    private static bool RoleHasPermission(string rolePermissionsJson, string requiredPermission)
    {
        if (string.IsNullOrEmpty(rolePermissionsJson)) return false;

        var permissions = JsonSerializer.Deserialize<List<string>>(rolePermissionsJson);
        if (permissions == null) return false;
        
        if (permissions.Contains("All")) return true;
        if (requiredPermission.EndsWith(".View") && permissions.Contains("All.View")) return true;
        if (permissions.Contains(requiredPermission)) return true;

        return false;
    }

    /// <summary>
    /// Traverses up the hierarchy (Bottom-Up) to gather the IDs of all parent units.
    /// Ex: Team -> Department -> Committee -> National.
    /// </summary>
    private async Task<HashSet<Guid>> GetPathToRootAsync(Guid startUnitId)
    {
        var path = new HashSet<Guid>();
        Guid? currentId = startUnitId;
        int maxDepth = 20;
        int currentDepth = 0;

        while (currentId.HasValue && currentDepth < maxDepth)
        {
            path.Add(currentId.Value);
            var parentId = await context.OrganizationUnits
                .Where(ou => ou.Id == currentId.Value)
                .Select(ou => ou.ParentUnitId)
                .FirstOrDefaultAsync();

            currentId = parentId;
            currentDepth++;
        }

        return path;
    }

    public async Task<bool> IsDirectMemberAsync(Guid userId, Guid unitId)
    {
        if (await IsSuperAdminAsync(userId)) return true;

        return await context.UserUnitRoles
            .AnyAsync(uur => uur.UserId == userId && uur.OrganizationUnitId == unitId);
    }
}

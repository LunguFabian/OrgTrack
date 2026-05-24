namespace OrgTrack.Api.Models;

public record UpdateMemberRoleRequest(string RoleName, Guid? TargetUnitId = null);

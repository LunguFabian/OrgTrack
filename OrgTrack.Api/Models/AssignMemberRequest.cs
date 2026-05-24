namespace OrgTrack.Api.Models;

/// <summary>
/// Request for assigning a user to an organizational unit.
/// UserEmail is the person's Google email (which we know from their Google account).
/// RoleName is the name of the role they will receive (e.g., "President", "VicePresident", "TeamLeader", "Member").
/// </summary>
public record AssignMemberRequest(
    string UserEmail,
    string RoleName
);

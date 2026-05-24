namespace OrgTrack.Application.DTOs;

/// <summary>
/// Information returned by the API when requesting the details of an invite link.
/// Will be used by the Frontend to display the screen: "You have been invited to the IT team by John. Click here to login."
/// </summary>
public record InviteLinkPreviewDto(
    string OrganizationUnitName,
    string RoleName,
    string CreatedByUserName,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsMaxUsesReached
);

/// <summary>
/// Information returned after successfully creating a link.
/// </summary>
public record InviteLinkCreatedDto(
    string Token,
    string InviteUrl, 
    DateTime ExpiresAt
);

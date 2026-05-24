namespace OrgTrack.Api.Models;

/// <summary>
/// HTTP request sent by a leader (e.g., VP or TeamLeader) to generate a recruitment link.
/// </summary>
public record GenerateInviteLinkRequest(
    Guid OrganizationUnitId,
    string RoleName,             // Ex: "Member"
    int ExpiresInHours = 48,     // Default validity: 48 hours
    int? MaxUses = null          // How many people can use the link (null = unlimited)
);

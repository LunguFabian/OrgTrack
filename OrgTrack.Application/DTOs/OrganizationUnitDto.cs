using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.DTOs;

/// <summary>
/// The DTO that represents an organizational unit.
/// The Children property allows building the tree view recursively on the frontend.
/// Members contains a simplified list of the members in this unit.
/// </summary>
public record OrganizationUnitDto(
    Guid Id,
    string Name,
    string Description,
    string Type,               // "National", "Committee", "Department", "Team"
    string DepartmentType,     // "None", "HR", "IT", "Finance" etc.
    Guid? ParentUnitId,
    DateTime CreatedAt,
    List<OrganizationUnitDto> Children,
    List<UnitMemberDto> Members
);

/// <summary>
/// Simplified DTO for a member of a unit.
/// Contains user data + the role they have in that unit.
/// </summary>
public record UnitMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string RoleName,
    DateTime JoinedAt,
    string? UnitName = null,
    Guid? UnitId = null
);

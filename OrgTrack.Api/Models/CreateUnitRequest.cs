namespace OrgTrack.Api.Models;

/// <summary>
/// Request for creating a new organizational unit.
/// ParentUnitId is null only for the National node (the root of the tree).
/// DepartmentType is mandatory only when Type = "Department".
/// </summary>
public record CreateUnitRequest(
    string Name,
    string Description,
    string Type,              // "National", "Committee", "Department", "Team"
    string? DepartmentType,   // "HR", "IT", "Finance" etc. (only for departments)
    Guid? ParentUnitId        // Parent unit ID (null = root)
);

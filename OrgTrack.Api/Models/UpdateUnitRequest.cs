namespace OrgTrack.Api.Models;

/// <summary>
/// Request for updating the name/description of a unit.
/// The Type or ParentUnitId cannot be changed through this endpoint
/// (moving in the tree would be a separate, more complex operation).
/// </summary>
public record UpdateUnitRequest(
    string Name,
    string Description
);

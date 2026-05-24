namespace OrgTrack.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Status,       // "ToDo", "InProgress", "WaitingForApproval", "Done"
    string Priority,     // "Low", "Medium", "High", "Critical"
    DateTime? Deadline,
    Guid OrganizationUnitId,
    string? AssigneeName,
    Guid? AssigneeId,
    string CreatorName,
    DateTime CreatedAt
);

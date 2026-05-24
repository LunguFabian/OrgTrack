using OrgTrack.Domain.Enums;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Api.Models;

public record CreateTaskRequest(
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? Deadline
);

public record UpdateTaskStatusRequest(
    TaskStatus NewStatus
);

public record UpdateTaskRequest(
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? Deadline
);

using System.Text.Json.Serialization;
using OrgTrack.Domain.Enums;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Api.Models;

public record CreateTaskRequest(
    string Title,
    string Description,
    [property: JsonRequired] TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? Deadline,
    Guid? ParentTaskId,
    TaskStatus? Status
);

public record UpdateTaskStatusRequest(
    [property: JsonRequired] TaskStatus NewStatus
);

public record UpdateTaskRequest(
    string Title,
    string Description,
    [property: JsonRequired] TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? Deadline,
    Guid? ParentTaskId
);

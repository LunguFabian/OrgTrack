namespace OrgTrack.Application.DTOs;

public record ActivityLogDto(
    DateTime Timestamp,
    string Action,
    string EntityType,
    string? Details,
    string? UnitName = null
);

public record MemberActivityScoreDto(
    Guid UserId,
    string UserName,
    int TasksDone,
    int EventsAttended,
    int TotalScore,
    string UnitName = "Unknown",
    string RoleName = "Unknown"
);

public record UnitActivitySummaryDto(
    Guid UnitId,
    string UnitName,
    int TasksDone,
    int EventsHeld,
    int MembersActive,
    IEnumerable<ActivityLogDto> RecentLogs
);

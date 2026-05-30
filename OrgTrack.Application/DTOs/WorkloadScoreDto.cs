namespace OrgTrack.Application.DTOs;

public record WorkloadScoreDto(
    Guid UserId,
    string UserName,
    string? PictureUrl,
    double FinalScore,
    double CurrentWorkloadRaw,
    double VelocityDaysRaw,
    int AffinityRaw,
    int DaysSinceLastAssignmentRaw,
    int SubtasksComplexityRaw
);

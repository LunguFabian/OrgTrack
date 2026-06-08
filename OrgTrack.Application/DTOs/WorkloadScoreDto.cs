namespace OrgTrack.Application.DTOs;

public record WorkloadScoreDto(
    Guid UserId,
    string UserName,
    string? PictureUrl,
    int Rank,
    double FinalScore,
    double CurrentWorkloadRaw,
    double AvgCompletionTimeRaw,
    int ThroughputRaw,
    int AvailabilityDaysRaw,
    int ComplexityLoadRaw,
    double ReliabilityRaw,
    double OverduePressureRaw,
    int CrossUnitLoadRaw
);

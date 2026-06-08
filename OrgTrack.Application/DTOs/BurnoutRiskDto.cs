using System;
using System.Collections.Generic;

namespace OrgTrack.Application.DTOs;

public record BurnoutRiskDto(
    Guid UserId,
    string UserName,
    string? PictureUrl,
    double BurnoutScorePercentage,
    string RiskLevel,
    List<string> WarningFlags
);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.DTOs;
using OrgTrack.Domain.Constants;
using OrgTrack.Infrastructure.Persistence;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController(
    AnalyticsService analyticsService,
    IPermissionService permissionService,
    OrganizationService organizationService,
    ReportService reportService) : ControllerBase
{
    /// <summary>
    /// The activity score of a specific volunteer (completed tasks + attendances).
    /// Accessible to anyone who has visibility in the respective unit.
    /// </summary>
    [HttpGet("members/{targetUserId:guid}")]
    public async Task<IActionResult> GetMemberScore(Guid targetUserId)
    {
        var currentUserId = User.GetUserId();
        
        if (currentUserId != targetUserId)
        {
            var targetUnits = await organizationService.GetMyUnitsAsync(targetUserId);
            bool hasAccess = false;
            foreach (var unit in targetUnits)
            {
                if (await permissionService.HasPermissionAsync(currentUserId, unit.Id, Permissions.MembersView))
                {
                    hasAccess = true;
                    break;
                }
            }
            if (!hasAccess) return Forbid();
        }

        var score = await analyticsService.GetMemberScoreAsync(targetUserId);
        return Ok(score);
    }

    /// <summary>
    /// Activity summary of an organizational unit (for the leader's dashboard).
    /// Requires Units.View permission for the respective unit.
    /// </summary>
    [HttpGet("units/{unitId:guid}")]
    public async Task<IActionResult> GetUnitSummary(Guid unitId)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.UnitsView))
        {
            return Forbid();
        }

        try
        {
            var summary = await analyticsService.GetUnitSummaryAsync(unitId);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// National dashboard: aggregate summary of all child committees.
    /// Accessible only to the National President (has the All permission = can see everything).
    /// </summary>
    [HttpGet("national/{nationalUnitId:guid}")]
    public async Task<IActionResult> GetNationalDashboard(Guid nationalUnitId)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, nationalUnitId, Permissions.UnitsManage))
        {
            return Forbid();
        }

        var summaries = await analyticsService.GetNationalDashboardAsync(nationalUnitId);
        return Ok(summaries);
    }

    [HttpGet("units/{unitId:guid}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid unitId)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.UnitsView))
        {
            return Forbid();
        }

        var members = await organizationService.GetMembersAsync(unitId);
        var scores = new List<MemberActivityScoreDto>();
        foreach (var member in members)
        {
            var score = await analyticsService.GetMemberScoreAsync(member.UserId);
            var roleName = member.RoleName ?? "Unknown";
            var formattedRole = System.Text.RegularExpressions.Regex.Replace(roleName, "(?<!^)([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));
            scores.Add(score with { UnitName = member.UnitName ?? "Unknown", RoleName = formattedRole });
        }

        var unit = await organizationService.GetUnitByIdAsync(unitId);
        if (unit != null && (unit.Type == "Committee" || unit.Type == "Department"))
        {
            scores = scores.OrderByDescending(s => s.TotalScore).Take(5).ToList();
        }
        else
        {
            scores = scores.OrderByDescending(s => s.TotalScore).ToList();
        }

        return Ok(scores);
    }

    /// <summary>
    /// Exports the unit's activity summary and member scores as a PDF or Excel report.
    /// Requires Units.View permission.
    /// </summary>
    [HttpGet("units/{unitId:guid}/report")]
    public async Task<IActionResult> GetReport(Guid unitId, [FromQuery] string format = "pdf")
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.UnitsView))
        {
            return Forbid();
        }

        try
        {
            var summary = await analyticsService.GetUnitSummaryAsync(unitId);
            
            // Get all unit members and their scores (NO TOP 5 LIMIT FOR REPORT)
            var members = await organizationService.GetMembersAsync(unitId);
            var scores = new List<MemberActivityScoreDto>();
            foreach (var member in members)
            {
                var score = await analyticsService.GetMemberScoreAsync(member.UserId);
                var roleName = member.RoleName ?? "Unknown";
                var formattedRole = System.Text.RegularExpressions.Regex.Replace(roleName, "(?<!^)([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));
                scores.Add(score with { UnitName = member.UnitName ?? "Unknown", RoleName = formattedRole });
            }

            var unit = await organizationService.GetUnitByIdAsync(unitId);
            bool showUnitColumn = unit != null && (unit.Type == "Committee" || unit.Type == "Department");

            if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                var excelBytes = reportService.GenerateExcelReportAsync(summary, scores, showUnitColumn);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OrgTrack_Report_{summary.UnitName}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            
            var pdfBytes = reportService.GeneratePdfReportAsync(summary, scores, showUnitColumn);
            return File(pdfBytes, "application/pdf", $"OrgTrack_Report_{summary.UnitName}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the burnout risk analysis for all members under the requester's hierarchical control.
    /// Only returns data for users that the requester has leadership over.
    /// </summary>
    [HttpGet("burnout-risks")]
    public async Task<IActionResult> GetHierarchicalBurnoutRisks([FromQuery] Guid? unitId = null)
    {
        var requesterId = User.GetUserId();
        var risks = await analyticsService.GetHierarchicalBurnoutRisksAsync(requesterId, unitId);
        
        // Exclude healthy users to only focus on risks
        var filteredRisks = risks.Where(r => r.RiskLevel != "Healthy").ToList();
        
        return Ok(filteredRisks);
    }
}

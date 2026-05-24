using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Constants;
using OrgTrack.Infrastructure.Persistence;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController(
    AnalyticsService analyticsService,
    IPermissionService permissionService,
    OrganizationService organizationService) : ControllerBase
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
}

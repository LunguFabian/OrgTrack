using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Api.Models;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Constants;

namespace OrgTrack.Api.Controllers;

[ApiController]
[Route("api/invite-links")]
public class InviteLinksController(
    InviteLinkService inviteLinkService,
    IPermissionService permissionService) : ControllerBase
{
    /// <summary>
    /// Generates a new invite link. Only users with the "Members.Manage" permission can do this.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> GenerateLink([FromBody] GenerateInviteLinkRequest request)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, request.OrganizationUnitId, Permissions.MembersManage))
        {
            return Forbid();
        }

        try
        {
            var result = await inviteLinkService.GenerateLinkAsync(
                userId,
                request.OrganizationUnitId,
                request.RoleName,
                request.ExpiresInHours,
                request.MaxUses
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the invite details (preview).
    /// This endpoint is PUBLIC [AllowAnonymous] because the visitor does not yet have an account when accessing the link for the first time.
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInviteDetails(string token)
    {
        var preview = await inviteLinkService.GetInviteDetailsAsync(token);
        if (preview == null)
        {
            return NotFound(new { error = "Invite link does not exist." });
        }

        return Ok(preview);
    }

    /// <summary>
    /// The user (who just authenticated with Google and has a JWT token) joins the unit using the link.
    /// </summary>
    [HttpPost("{token}/join")]
    [Authorize]
    public async Task<IActionResult> JoinViaLink(string token)
    {
        var userId = User.GetUserId();

        try
        {
            await inviteLinkService.JoinViaLinkAsync(token, userId);
            return Ok(new { message = "Te-ai alăturat cu succes echipei!" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

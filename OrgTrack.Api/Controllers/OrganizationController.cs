using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Api.Models;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Domain.Constants;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/organization")]
public class OrganizationController(
    OrganizationService organizationService,
    IPermissionService permissionService,
    IUserRepository userRepository) : ControllerBase
{
    /// <summary>
    /// Creates a new organizational unit.
    /// For example: the National President creates a Committee, then the LCP creates Departments.
    /// </summary>
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request)
    {
        var userId = User.GetUserId();
        if (request.ParentUnitId.HasValue && 
            !await permissionService.HasPermissionAsync(
                userId, 
                request.ParentUnitId.Value,
                Permissions.UnitsManage))
        {
            return Forbid();
        }
        
        try
        {
            var unit = await organizationService.CreateUnitAsync(
                request.Name,
                request.Description,
                request.Type,
                request.DepartmentType,
                request.ParentUnitId,
                userId
            );

            return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, unit);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns a unit with its direct children and members.
    /// Accessible by: anyone with Units.View or Tasks.Manage via hierarchy (VPs, TLs),
    /// OR any direct member of that unit (so Members can see their own team page).
    /// </summary>
    [HttpGet("units/{id:guid}")]
    public async Task<IActionResult> GetUnit(Guid id)
    {
        var userId = User.GetUserId();
        bool hasHierarchyAccess = await permissionService.HasPermissionAsync(userId, id, Permissions.UnitsView)
            || await permissionService.HasPermissionAsync(userId, id, Permissions.TasksManage)
            || await permissionService.HasPermissionAsync(userId, id, Permissions.TasksView)
            || await permissionService.HasPermissionAsync(userId, id, Permissions.MembersView);
        bool isDirectMember = await permissionService.IsDirectMemberAsync(userId, id);

        if (!hasHierarchyAccess && !isDirectMember)
        {
            return Forbid();
        }

        var unit = await organizationService.GetUnitByIdAsync(id);
        if (unit == null)
        {
            return NotFound(new { error = "Unit not found." });
        }
        return Ok(unit);
    }

    /// <summary>
    /// Grabs the entire organization tree structure.
    /// Used for the Tree View on the frontend.
    /// Flows from National -> Committees -> Departments -> Teams (along with their members).
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var tree = await organizationService.GetFullTreeAsync();
        return Ok(tree);
    }

    [HttpGet("/api/me/units")]
    public async Task<IActionResult> GetMyUnits()
    {
        var userId = User.GetUserId();
        var units = await organizationService.GetMyUnitsAsync(userId);
        return Ok(units);
    }

    /// <summary>
    /// Updates the name and description of a unit.
    /// </summary>
    [HttpPut("units/{id:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest request)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, id, Permissions.UnitsManage))
        {
            return Forbid();
        }

        var unit = await organizationService.UpdateUnitAsync(id, request.Name, request.Description);
        if (unit == null)
        {
            return NotFound(new { error = "Unitatea nu a fost găsită." });
        }
        return Ok(unit);
    }

    /// <summary>
    /// Deletes an organizational unit.
    /// Note: You can't delete a unit that still has children (sub-units must be deleted first).
    /// </summary>
    [HttpDelete("units/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, id, Permissions.UnitsManage))
        {
            return Forbid();
        }

        try
        {
            var deleted = await organizationService.DeleteUnitAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "Unitatea nu a fost găsită." });
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    /// <summary>
    /// Assigns a user to a unit with a specific role.
    /// Example: POST /api/organization/units/{id}/members
    /// Body: { "userEmail": "ion@gmail.com", "roleName": "TeamLeader" }
    /// </summary>
    [HttpPost("units/{unitId:guid}/members")]
    public async Task<IActionResult> AssignMember(Guid unitId, [FromBody] AssignMemberRequest request)
    {
        var userId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(userId, unitId, Permissions.MembersManage))
        {
            return Forbid();
        }

        try
        {
            var member = await organizationService.AssignMemberAsync(
                unitId, request.UserEmail, request.RoleName, userId
            );
            return Ok(member);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates the role of an existing member within a unit, optionally moving them to a new unit.
    /// </summary>
    [HttpPut("units/{unitId:guid}/members/{targetUserId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid unitId, Guid targetUserId, [FromBody] UpdateMemberRoleRequest request)
    {
        var currentUserId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(currentUserId, unitId, Permissions.MembersManage))
        {
            return Forbid();
        }

        try
        {
            var updated = await organizationService.UpdateMemberRoleAsync(unitId, targetUserId, request.RoleName, currentUserId, request.TargetUnitId);
            if (!updated)
                return NotFound(new { error = "Member not found in this unit." });
            return Ok(new { message = "Role updated successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists all members of a unit with their roles.
    /// </summary>
    [HttpGet("units/{unitId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid unitId)
    {
        var userId = User.GetUserId();
        bool hasPermission = await permissionService.HasPermissionAsync(userId, unitId, Permissions.MembersView)
            || await permissionService.HasPermissionAsync(userId, unitId, Permissions.MembersManage)
            || await permissionService.HasPermissionAsync(userId, unitId, Permissions.TasksManage);
        bool isDirectMember = await permissionService.IsDirectMemberAsync(userId, unitId);

        if (!hasPermission && !isDirectMember)
        {
            return Forbid();
        }

        var members = await organizationService.GetMembersAsync(unitId);
        return Ok(members);
    }

    /// <summary>
    /// Removes a user from a unit.
    /// </summary>
    [HttpDelete("units/{unitId:guid}/members/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid unitId, Guid targetUserId)
    {
        var currentUserId = User.GetUserId();
        if (!await permissionService.HasPermissionAsync(currentUserId, unitId, Permissions.MembersManage))
        {
            return Forbid();
        }

        var removed = await organizationService.RemoveMemberAsync(unitId, targetUserId);
        if (!removed)
        {
            return NotFound(new { error = "Member not found in this unit." });
        }
        return NoContent();
    }

    [HttpGet("search-members")]
    public async Task<IActionResult> SearchMembers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new List<object>());
        }

        var users = await userRepository.SearchUsersAsync(q);
        var result = users.Select(u => new
        {
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.PictureUrl
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Returns the public profile of a user: name, email, avatar, and their unit memberships.
    /// </summary>
    [HttpGet("users/{id:guid}/profile")]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });

        var roles = await organizationService.GetMyUnitsAsync(id);
        
        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PictureUrl,
            Units = roles.Select(u => new
            {
                u.Id,
                u.Name,
                u.Type,
                u.DepartmentType,
                RoleName = u.Members?.FirstOrDefault(m => m.UserId == id)?.RoleName ?? "Member"
            })
        });
    }
}

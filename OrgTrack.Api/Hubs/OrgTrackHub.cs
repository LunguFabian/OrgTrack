using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrgTrack.Api.Hubs;

[Authorize]
public class OrgTrackHub : Hub
{
    /// <summary>
    /// When a client connects, automatically join them to their personal notification group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Join a unit group to receive real-time task/event updates for that unit.
    /// Called by the client when navigating to a unit's Kanban board.
    /// </summary>
    public async Task JoinUnitGroup(string unitId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Unit_{unitId}");
    }

    /// <summary>
    /// Leave a unit group. Called when navigating away from the unit.
    /// </summary>
    public async Task LeaveUnitGroup(string unitId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Unit_{unitId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

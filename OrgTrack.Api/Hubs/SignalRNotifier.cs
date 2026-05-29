using Microsoft.AspNetCore.SignalR;
using OrgTrack.Application.Interfaces;

namespace OrgTrack.Api.Hubs;

/// <summary>
/// SignalR implementation of IRealtimeNotifier.
/// </summary>
public class SignalRNotifier(IHubContext<OrgTrackHub> hubContext) : IRealtimeNotifier
{
    public async Task SendToUserAsync(Guid userId, string method, object data)
    {
        await hubContext.Clients.Group($"User_{userId}").SendAsync(method, data);
    }

    public async Task SendToGroupAsync(string groupName, string method, object data)
    {
        await hubContext.Clients.Group(groupName).SendAsync(method, data);
    }
}

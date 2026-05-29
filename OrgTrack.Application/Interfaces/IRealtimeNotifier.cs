namespace OrgTrack.Application.Interfaces;

/// <summary>
/// Abstraction for sending real-time notifications via WebSockets.
/// Implemented by SignalR in the API layer.
/// </summary>
public interface IRealtimeNotifier
{
    Task SendToUserAsync(Guid userId, string method, object data);
    Task SendToGroupAsync(string groupName, string method, object data);
}

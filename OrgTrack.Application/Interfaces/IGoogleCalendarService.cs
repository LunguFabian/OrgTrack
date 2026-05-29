using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public class EventCalendarData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public List<string>? AttendeeEmails { get; set; }
}

public interface IGoogleCalendarService
{
    /// <summary>
    /// Creates an event in the user's primary Google Calendar and returns the Google Event ID.
    /// </summary>
    Task<string?> CreateEventAsync(string accessToken, string refreshToken, EventCalendarData data);
    
    /// <summary>
    /// Updates an existing event in the Google Calendar.
    /// </summary>
    Task UpdateEventAsync(string accessToken, string refreshToken, string calendarEventId, EventCalendarData data);
    
    /// <summary>
    /// Deletes an existing event from the Google Calendar.
    /// </summary>
    Task DeleteEventAsync(string accessToken, string refreshToken, string calendarEventId);
}

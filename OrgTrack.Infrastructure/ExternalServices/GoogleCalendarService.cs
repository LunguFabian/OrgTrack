using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using OrgTrack.Application.Interfaces;

namespace OrgTrack.Infrastructure.ExternalServices;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GoogleCalendarService(IConfiguration configuration)
    {
        _clientId = configuration["Google:ClientId"] ?? throw new ArgumentNullException("Google:ClientId");
        _clientSecret = configuration["Google:ClientSecret"] ?? throw new ArgumentNullException("Google:ClientSecret");
    }

    private CalendarService GetCalendarService(string accessToken, string refreshToken)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            },
            Scopes = new[] { CalendarService.Scope.CalendarEvents }
        });

        var token = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        var credential = new UserCredential(flow, "user", token);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "OrgTrack"
        });
    }

    public async Task<string?> CreateEventAsync(string accessToken, string refreshToken, EventCalendarData data)
    {
        try
        {
            var service = GetCalendarService(accessToken, refreshToken);

            var newEvent = new Event
            {
                Summary = data.Title,
                Description = data.Description,
                Start = new EventDateTime { DateTimeDateTimeOffset = data.StartDate, TimeZone = "UTC" },
                End = new EventDateTime { DateTimeDateTimeOffset = data.EndDate, TimeZone = "UTC" },
            };

            if (data.IsRecurring && !string.IsNullOrEmpty(data.RecurrencePattern))
            {
                var rrule = data.RecurrencePattern.ToUpper() switch
                {
                    "BIWEEKLY" => "RRULE:FREQ=WEEKLY;INTERVAL=2",
                    "MONTHLY" => "RRULE:FREQ=MONTHLY",
                    _ => "RRULE:FREQ=WEEKLY"
                };
                newEvent.Recurrence = new List<string> { rrule };
            }

            if (data.AttendeeEmails != null && data.AttendeeEmails.Any())
            {
                newEvent.Attendees = data.AttendeeEmails.Select(email => new EventAttendee { Email = email }).ToList();
            }

            var request = service.Events.Insert(newEvent, "primary");
            var createdEvent = await request.ExecuteAsync();

            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating Google Calendar event: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateEventAsync(string accessToken, string refreshToken, string calendarEventId, EventCalendarData data)
    {
        try
        {
            var service = GetCalendarService(accessToken, refreshToken);

            var eventToUpdate = await service.Events.Get("primary", calendarEventId).ExecuteAsync();
            if (eventToUpdate == null) return;

            eventToUpdate.Summary = data.Title;
            eventToUpdate.Description = data.Description;
            eventToUpdate.Start = new EventDateTime { DateTimeDateTimeOffset = data.StartDate, TimeZone = "UTC" };
            eventToUpdate.End = new EventDateTime { DateTimeDateTimeOffset = data.EndDate, TimeZone = "UTC" };

            if (data.IsRecurring && !string.IsNullOrEmpty(data.RecurrencePattern))
            {
                var rrule = data.RecurrencePattern.ToUpper() switch
                {
                    "BIWEEKLY" => "RRULE:FREQ=WEEKLY;INTERVAL=2",
                    "MONTHLY" => "RRULE:FREQ=MONTHLY",
                    _ => "RRULE:FREQ=WEEKLY"
                };
                eventToUpdate.Recurrence = new List<string> { rrule };
            }
            else
            {
                eventToUpdate.Recurrence = null;
            }

            if (data.AttendeeEmails != null && data.AttendeeEmails.Any())
            {
                eventToUpdate.Attendees = data.AttendeeEmails.Select(email => new EventAttendee { Email = email }).ToList();
            }
            else
            {
                eventToUpdate.Attendees = null;
            }

            var request = service.Events.Update(eventToUpdate, "primary", calendarEventId);
            await request.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Google Calendar event: {ex.Message}");
        }
    }

    public async Task DeleteEventAsync(string accessToken, string refreshToken, string calendarEventId)
    {
        try
        {
            var service = GetCalendarService(accessToken, refreshToken);
            var request = service.Events.Delete("primary", calendarEventId);
            await request.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting Google Calendar event: {ex.Message}");
        }
    }
}

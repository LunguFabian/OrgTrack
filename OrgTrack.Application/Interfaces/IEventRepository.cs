using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<IEnumerable<Event>> GetByUnitIdAsync(Guid unitId);
    Task<IEnumerable<Event>> GetByUnitIdsAsync(IEnumerable<Guid> unitIds);
    Task<IEnumerable<Event>> GetVisibleEventsAsync(Guid userId, IEnumerable<Guid> userUnitIds);
    Task AddAsync(Event newEvent);
    Task UpdateAsync(Event updatedEvent);
    Task DeleteAsync(Event deletedEvent);
    Task<EventRsvp?> GetRsvpAsync(Guid eventId, Guid userId);
    Task<IEnumerable<EventRsvp>> GetUserRsvpsAsync(Guid userId, IEnumerable<Guid> eventIds);
    Task AddRsvpAsync(EventRsvp rsvp);
    Task UpdateRsvpAsync(EventRsvp rsvp);
    Task<IEnumerable<EventRsvp>> GetAttendanceReportAsync(Guid eventId);
}

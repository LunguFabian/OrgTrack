using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class EventRepository(OrgTrackDbContext context) : IEventRepository
{
    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await context.Events
            .Include(e => e.OrganizationUnit)
            .Include(e => e.InvitedUnits)
            .Include(e => e.InvitedUsers)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Event>> GetByUnitIdAsync(Guid unitId)
    {
        return await context.Events
            .Include(e => e.InvitedUnits)
            .Include(e => e.InvitedUsers)
            .Where(e => e.OrganizationUnitId == unitId)
            .OrderByDescending(e => e.StartDate) // Cele mai recente/viitoare evenimente apar primele
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByUnitIdsAsync(IEnumerable<Guid> unitIds)
    {
        var unitIdList = unitIds.ToList();
        return await context.Events
            .Include(e => e.InvitedUnits)
            .Include(e => e.InvitedUsers)
            .Where(e => unitIdList.Contains(e.OrganizationUnitId))
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetVisibleEventsAsync(Guid userId, IEnumerable<Guid> userUnitIds)
    {
        var unitIdList = userUnitIds.ToList();
        return await context.Events
            .Include(e => e.InvitedUnits)
            .Include(e => e.InvitedUsers)
            .Where(e => e.InvitedUsers.Any(u => u.UserId == userId) || 
                        e.InvitedUnits.Any(u => unitIdList.Contains(u.OrganizationUnitId)))
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();
    }

    public async Task AddAsync(Event newEvent)
    {
        context.Events.Add(newEvent);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event updatedEvent)
    {
        context.Events.Update(updatedEvent);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Event deletedEvent)
    {
        context.Events.Remove(deletedEvent);
        await context.SaveChangesAsync();
    }

    public async Task<EventRsvp?> GetRsvpAsync(Guid eventId, Guid userId)
    {
        return await context.EventRsvps
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
    }

    public async Task<IEnumerable<EventRsvp>> GetUserRsvpsAsync(Guid userId, IEnumerable<Guid> eventIds)
    {
        var eventIdList = eventIds.ToList();
        return await context.EventRsvps
            .Where(r => r.UserId == userId && eventIdList.Contains(r.EventId))
            .ToListAsync();
    }

    public async Task AddRsvpAsync(EventRsvp rsvp)
    {
        context.EventRsvps.Add(rsvp);
        await context.SaveChangesAsync();
    }

    public async Task UpdateRsvpAsync(EventRsvp rsvp)
    {
        context.EventRsvps.Update(rsvp);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EventRsvp>> GetAttendanceReportAsync(Guid eventId)
    {
        return await context.EventRsvps
            .Include(r => r.User)
            .Where(r => r.EventId == eventId)
            .ToListAsync();
    }
}

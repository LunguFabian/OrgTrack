using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Application.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly Mock<IGoogleCalendarService> _googleCalendarServiceMock;
    private readonly ActivityLogService _activityLogService;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _googleCalendarServiceMock = new Mock<IGoogleCalendarService>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object, new Mock<IUserRepository>().Object);

        _eventService = new EventService(
            _eventRepositoryMock.Object,
            _unitRepositoryMock.Object,
            _userRepositoryMock.Object,
            _activityLogService,
            _googleCalendarServiceMock.Object,
            new NotificationService(new Mock<INotificationRepository>().Object, new Mock<IRealtimeNotifier>().Object)
        );
    }

    [Fact]
    public async Task CreateEventAsync_ShouldCreateEvent()
    {
        var unitId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit { Id = unitId });
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);

        var result = await _eventService.CreateEventAsync(unitId, "Test Event", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, null, null, creatorId);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldUpdateEvent()
    {
        var eventId = Guid.NewGuid();
        var ev = new Event { Id = eventId, Title = "Old", OrganizationUnitId = Guid.NewGuid() };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        _eventRepositoryMock.Setup(r => r.UpdateAsync(ev)).Returns(Task.CompletedTask);

        var result = await _eventService.UpdateEventAsync(Guid.NewGuid(), eventId, "New", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("New");
    }

    [Fact]
    public async Task CreateEventAsync_ShouldThrowException_WhenUnitNotFound()
    {
        var unitId = Guid.NewGuid();
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync((OrganizationUnit?)null);

        var action = async () => await _eventService.CreateEventAsync(
            unitId, "Title", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, null, null, Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Organization unit not found.");
    }

    [Fact]
    public async Task CreateEventAsync_ShouldThrowException_WhenEndDateIsBeforeStartDate()
    {
        var unitId = Guid.NewGuid();
        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(new OrganizationUnit { Id = unitId });

        var action = async () => await _eventService.CreateEventAsync(
            unitId, "Title", "Desc", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null, null, Guid.NewGuid());

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("End date must be after the start date.");
    }

    [Fact]
    public async Task CreateEventAsync_ShouldSyncWithGoogleCalendar_WhenUserIsConnected()
    {
        var unitId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var unit = new OrganizationUnit { Id = unitId };
        var creator = new User { Id = creatorId, IsGoogleCalendarConnected = true, GoogleCalendarAccessToken = "token" };

        _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(creatorId)).ReturnsAsync(creator);
        
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        
        Event? savedEvent = null;
        _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>()))
            .Callback<Event>(e => savedEvent = e)
            .Returns(Task.CompletedTask);

        _googleCalendarServiceMock.Setup(r => r.CreateEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EventCalendarData>()))
            .ReturnsAsync("google_id");

        var result = await _eventService.CreateEventAsync(
            unitId, "Title", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, null, null, creatorId);

        result.Should().NotBeNull();
        savedEvent.Should().NotBeNull();
        savedEvent!.ExternalCalendarId.Should().Be("google_id");
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldThrowException_WhenEventNotFound()
    {
        var eventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var action = async () => await _eventService.UpdateEventAsync(
            Guid.NewGuid(), eventId, "New", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, null, null);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Event not found.");
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldThrowException_WhenEventNotFound()
    {
        var eventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var action = async () => await _eventService.DeleteEventAsync(Guid.NewGuid(), eventId);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Event not found.");
    }

    [Fact]
    public async Task GetMyEventsAsync_ShouldReturnEvents()
    {
        var userId = Guid.NewGuid();
        var ev = new Event { Id = Guid.NewGuid(), Title = "My Event" };
        _eventRepositoryMock.Setup(r => r.GetVisibleEventsAsync(userId, It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<Event> { ev });
        _eventRepositoryMock.Setup(r => r.GetUserRsvpsAsync(userId, It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<EventRsvp>());
        _unitRepositoryMock.Setup(r => r.GetUserRolesAsync(userId)).ReturnsAsync(new List<UserUnitRole> { new UserUnitRole { OrganizationUnitId = Guid.NewGuid() } });
        _unitRepositoryMock.Setup(r => r.GetAncestorUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());

        var result = await _eventService.GetMyEventsAsync(userId);

        var resultList = result.ToList();
        resultList.Should().HaveCount(1);
        resultList[0].Title.Should().Be("My Event");
    }

    [Fact]
    public async Task GetEventsByUnitAsync_ShouldReturnEvents()
    {
        var unitId = Guid.NewGuid();
        var ev = new Event { Id = Guid.NewGuid(), Title = "Unit Event" };
        _eventRepositoryMock.Setup(r => r.GetByUnitIdAsync(unitId)).ReturnsAsync(new List<Event> { ev });
        _eventRepositoryMock.Setup(r => r.GetUserRsvpsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<EventRsvp>());

        var result = await _eventService.GetEventsByUnitAsync(unitId, Guid.NewGuid());

        var resultList = result.ToList();
        resultList.Should().HaveCount(1);
        resultList[0].Title.Should().Be("Unit Event");
    }

    [Fact]
    public async Task RsvpAsync_ShouldUpdateStatus()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ev = new Event { Id = eventId };
        
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());

        _eventRepositoryMock.Setup(r => r.GetRsvpAsync(eventId, userId)).ReturnsAsync((EventRsvp?)null);
        _eventRepositoryMock.Setup(r => r.AddRsvpAsync(It.IsAny<EventRsvp>())).Returns(Task.CompletedTask);

        await _eventService.SetRsvpAsync(eventId, userId, RsvpStatus.Going);

        _eventRepositoryMock.Verify(r => r.AddRsvpAsync(It.IsAny<EventRsvp>()), Times.Once);
    }
}

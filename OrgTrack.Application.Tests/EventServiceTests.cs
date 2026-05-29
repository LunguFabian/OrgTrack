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
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object);

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
    public async Task DeleteEventAsync_ShouldDeleteEvent()
    {
        var eventId = Guid.NewGuid();
        var ev = new Event { Id = eventId, OrganizationUnitId = Guid.NewGuid() };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _eventRepositoryMock.Setup(r => r.DeleteAsync(ev)).Returns(Task.CompletedTask);

        await _eventService.DeleteEventAsync(Guid.NewGuid(), eventId);

        _eventRepositoryMock.Verify(r => r.DeleteAsync(ev), Times.Once);
    }
}

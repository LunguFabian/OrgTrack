using FluentAssertions;
using Moq;
using OrgTrack.Application.UseCases;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;
using Xunit;

namespace OrgTrack.Application.Tests;

public class EventServiceExtendedTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IOrganizationUnitRepository> _unitRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly Mock<IGoogleCalendarService> _googleCalendarServiceMock;
    private readonly ActivityLogService _activityLogService;
    private readonly EventService _sut;

    public EventServiceExtendedTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _unitRepositoryMock = new Mock<IOrganizationUnitRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _googleCalendarServiceMock = new Mock<IGoogleCalendarService>();
        _activityLogService = new ActivityLogService(_activityLogRepositoryMock.Object, new Mock<IUserRepository>().Object);

        _sut = new EventService(
            _eventRepositoryMock.Object,
            _unitRepositoryMock.Object,
            _userRepositoryMock.Object,
            _activityLogService,
            _googleCalendarServiceMock.Object,
            new NotificationService(new Mock<INotificationRepository>().Object, new Mock<IRealtimeNotifier>().Object)
        );
    }

    [Fact]
    public async Task SetAttendanceAsync_ShouldAddNewRsvp_WhenNoneExists()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Id = eventId });
        _eventRepositoryMock.Setup(r => r.GetRsvpAsync(eventId, userId)).ReturnsAsync((EventRsvp?)null);

        await _sut.SetAttendanceAsync(eventId, userId, AttendanceStatus.Present);

        _eventRepositoryMock.Verify(r => r.AddRsvpAsync(It.Is<EventRsvp>(rsvp => rsvp.Attendance == AttendanceStatus.Present)), Times.Once);
    }

    [Fact]
    public async Task SetAttendanceAsync_ShouldUpdateExistingRsvp_WhenExists()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new EventRsvp { EventId = eventId, UserId = userId, Attendance = AttendanceStatus.Unmarked };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Id = eventId });
        _eventRepositoryMock.Setup(r => r.GetRsvpAsync(eventId, userId)).ReturnsAsync(existing);

        await _sut.SetAttendanceAsync(eventId, userId, AttendanceStatus.Present);

        _eventRepositoryMock.Verify(r => r.UpdateRsvpAsync(It.Is<EventRsvp>(rsvp => rsvp.Attendance == AttendanceStatus.Present)), Times.Once);
    }

    [Fact]
    public async Task GetAttendanceReportAsync_ShouldReturnReport()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ev = new Event { Id = eventId, InvitedUsers = new List<EventInvitedUser> { new EventInvitedUser { UserId = userId } } };
        var user = new User { Id = userId, FirstName = "John", LastName = "Doe" };
        
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());

        _eventRepositoryMock.Setup(r => r.GetAttendanceReportAsync(eventId)).ReturnsAsync(new List<EventRsvp>
        {
            new EventRsvp { UserId = userId, EventId = eventId, Rsvp = RsvpStatus.Going, Attendance = AttendanceStatus.Present }
        });

        var result = await _sut.GetAttendanceReportAsync(eventId);

        result.Should().HaveCount(1);
        result.First().UserName.Should().Be("John Doe");
        result.First().Attendance.Should().Be("Present");
        result.First().Rsvp.Should().Be("Going");
    }

    [Fact]
    public async Task GetRsvpSummaryAsync_ShouldReturnSummary()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ev = new Event { Id = eventId, InvitedUsers = new List<EventInvitedUser> { new EventInvitedUser { UserId = userId } } };
        var user = new User { Id = userId, FirstName = "Jane", LastName = "Doe" };
        
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());

        _eventRepositoryMock.Setup(r => r.GetAttendanceReportAsync(eventId)).ReturnsAsync(new List<EventRsvp>
        {
            new EventRsvp { UserId = userId, EventId = eventId, Rsvp = RsvpStatus.Maybe }
        });

        var result = await _sut.GetRsvpSummaryAsync(eventId);

        result.Should().HaveCount(1);
        result.First().UserName.Should().Be("Jane Doe");
        result.First().Rsvp.Should().Be("Maybe");
    }

    [Fact]
    public async Task IsUserEligibleForEventAsync_ShouldReturnTrue_WhenEligible()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ev = new Event { Id = eventId, InvitedUsers = new List<EventInvitedUser> { new EventInvitedUser { UserId = userId } } };
        var user = new User { Id = userId };
        
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitRepositoryMock.Setup(r => r.GetMembersForUnitsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<UserUnitRole>());
        _unitRepositoryMock.Setup(r => r.GetDescendantUnitIdsAsync(It.IsAny<Guid>())).ReturnsAsync(new HashSet<Guid>());

        var result = await _sut.IsUserEligibleForEventAsync(eventId, userId);

        result.Should().BeTrue();
    }
}

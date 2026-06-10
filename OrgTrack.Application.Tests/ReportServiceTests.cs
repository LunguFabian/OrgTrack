using FluentAssertions;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.UseCases;
using QuestPDF.Infrastructure;
using Xunit;

namespace OrgTrack.Application.Tests;

public class ReportServiceTests
{
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _sut = new ReportService();
    }

    [Fact]
    public void GeneratePdfReportAsync_ShouldReturnPdfBytes_WhenDataIsProvided()
    {
        // Arrange
        var summary = new UnitActivitySummaryDto(
            Guid.NewGuid(),
            "Test Unit",
            10,
            5,
            20,
            new List<ActivityLogDto>
            {
                new ActivityLogDto(DateTime.UtcNow, "Task Completed", "Task", "Task 1 completed by User")
            }
        );

        var scores = new List<MemberActivityScoreDto>
        {
            new MemberActivityScoreDto(Guid.NewGuid(), "Test User", 5, 2, 100, "Test Unit", "Member")
        };

        var burnoutRisks = new List<BurnoutRiskDto>
        {
            new BurnoutRiskDto(Guid.NewGuid(), "Test User", null, 80, "High", new List<string> { "High Workload" })
        };

        // Act
        var result = _sut.GeneratePdfReportAsync(summary, scores, burnoutRisks, showUnitColumn: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // PDF magic number %PDF-
        result.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    [Fact]
    public void GenerateExcelReportAsync_ShouldReturnExcelBytes_WhenDataIsProvided()
    {
        // Arrange
        var summary = new UnitActivitySummaryDto(
            Guid.NewGuid(),
            "Test Unit",
            10,
            5,
            20,
            new List<ActivityLogDto>
            {
                new ActivityLogDto(DateTime.UtcNow, "Task Completed", "Task", "Test Unit", "Task 1 completed by User")
            }
        );

        var scores = new List<MemberActivityScoreDto>
        {
            new MemberActivityScoreDto(Guid.NewGuid(), "Test User", 5, 2, 100, "Test Unit", "Member")
        };

        var burnoutRisks = new List<BurnoutRiskDto>
        {
            new BurnoutRiskDto(Guid.NewGuid(), "Test User", null, 80, "High", new List<string> { "High Workload" })
        };

        // Act
        var result = _sut.GenerateExcelReportAsync(summary, scores, burnoutRisks, showUnitColumn: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // ZIP magic number PK (Excel xlsx are zipped XMLs)
        result.Take(2).Should().BeEquivalentTo(new byte[] { 0x50, 0x4B });
    }
    
    [Fact]
    public void GeneratePdfReportAsync_ShouldWork_WithoutBurnoutRisks()
    {
        // Arrange
        var summary = new UnitActivitySummaryDto(Guid.NewGuid(), "Test Unit", 0, 0, 0, new List<ActivityLogDto>());
        var scores = new List<MemberActivityScoreDto>();
        var burnoutRisks = new List<BurnoutRiskDto>();

        // Act
        var result = _sut.GeneratePdfReportAsync(summary, scores, burnoutRisks, showUnitColumn: false);

        // Assert
        result.Should().NotBeEmpty();
    }
    
    [Fact]
    public void GenerateExcelReportAsync_ShouldWork_WithoutBurnoutRisks()
    {
        // Arrange
        var summary = new UnitActivitySummaryDto(Guid.NewGuid(), "Test Unit", 0, 0, 0, new List<ActivityLogDto>());
        var scores = new List<MemberActivityScoreDto>();
        var burnoutRisks = new List<BurnoutRiskDto>();

        // Act
        var result = _sut.GenerateExcelReportAsync(summary, scores, burnoutRisks, showUnitColumn: false);

        // Assert
        result.Should().NotBeEmpty();
    }
}

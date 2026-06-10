using ClosedXML.Excel;
using OrgTrack.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrgTrack.Application.UseCases;

public class ReportService
{
    // Frontend branding colors (Emerald-500)
    private readonly string _primaryColor = "#10b981"; // Emerald-500
    private readonly string _textColor = "#1f2937"; // Gray-800
    private readonly string _mutedColor = "#6b7280"; // Gray-500
    private readonly string _borderColor = "#e5e7eb"; // Gray-200
    private readonly string _surfaceColor = "#f3f4f6"; // Gray-100

    private const string LogoSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""32"" height=""32"" viewBox=""0 0 32 32"">
  <rect width=""32"" height=""32"" rx=""8"" fill=""#10b981""/>
  <svg x=""6"" y=""6"" width=""20"" height=""20"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#ffffff"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"">
    <rect x=""16"" y=""16"" width=""6"" height=""6"" rx=""1""/>
    <rect x=""2"" y=""16"" width=""6"" height=""6"" rx=""1""/>
    <rect x=""9"" y=""2"" width=""6"" height=""6"" rx=""1""/>
    <path d=""M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3""/>
    <path d=""M12 12V8""/>
  </svg>
</svg>";

    public byte[] GeneratePdfReportAsync(UnitActivitySummaryDto summary, List<MemberActivityScoreDto> scores, List<BurnoutRiskDto> burnoutRisks, bool showUnitColumn = false)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial).FontColor(_textColor));

                page.Header().Element(c => ComposeHeader(c, summary));
                page.Content().Element(c => ComposeContent(c, summary, scores, burnoutRisks, showUnitColumn));
                page.Footer().Element(ComposeFooter);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public byte[] GenerateExcelReportAsync(UnitActivitySummaryDto summary, List<MemberActivityScoreDto> scores, List<BurnoutRiskDto> burnoutRisks, bool showUnitColumn = false)
    {
        using var workbook = new XLWorkbook();
        
        GenerateExcelSummarySheet(workbook, summary);
        GenerateExcelLeaderboardSheet(workbook, scores, showUnitColumn);
        GenerateExcelLogSheet(workbook, summary);
        
        if (burnoutRisks.Any())
        {
            GenerateExcelBurnoutSheet(workbook, burnoutRisks);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void GenerateExcelSummarySheet(XLWorkbook workbook, UnitActivitySummaryDto summary)
    {
        var wsSummary = workbook.Worksheets.Add("Summary");
        wsSummary.Cell("A1").Value = "OrgTrack Activity Report";
        wsSummary.Cell("A1").Style.Font.Bold = true;
        wsSummary.Cell("A1").Style.Font.FontSize = 16;
        
        wsSummary.Cell("A3").Value = "Unit Name:";
        wsSummary.Cell("B3").Value = summary.UnitName;
        wsSummary.Cell("A4").Value = "Generated On:";
        wsSummary.Cell("B4").Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC");
        
        wsSummary.Cell("A6").Value = "Metric";
        wsSummary.Cell("B6").Value = "Value";
        wsSummary.Range("A6:B6").Style.Font.Bold = true;
        wsSummary.Range("A6:B6").Style.Fill.BackgroundColor = XLColor.FromHtml(_primaryColor);
        wsSummary.Range("A6:B6").Style.Font.FontColor = XLColor.White;

        wsSummary.Cell("A7").Value = "Tasks Completed";
        wsSummary.Cell("B7").Value = summary.TasksDone;
        wsSummary.Cell("A8").Value = "Events Held";
        wsSummary.Cell("B8").Value = summary.EventsHeld;
        wsSummary.Cell("A9").Value = "Active Members";
        wsSummary.Cell("B9").Value = summary.MembersActive;

        wsSummary.Columns().AdjustToContents();
    }

    private void GenerateExcelLeaderboardSheet(XLWorkbook workbook, List<MemberActivityScoreDto> scores, bool showUnitColumn)
    {
        var wsLeaderboard = workbook.Worksheets.Add("Leaderboard");
        wsLeaderboard.Cell("A1").Value = "Rank";
        wsLeaderboard.Cell("B1").Value = "Name";
        wsLeaderboard.Cell("C1").Value = "Role";

        int dataStartCol = 4;
        if (showUnitColumn)
        {
            wsLeaderboard.Cell("D1").Value = "Unit";
            dataStartCol = 5;
        }

        wsLeaderboard.Cell(1, dataStartCol).Value = "Tasks Done";
        wsLeaderboard.Cell(1, dataStartCol + 1).Value = "Events Attended";
        wsLeaderboard.Cell(1, dataStartCol + 2).Value = "Total Score";

        var headerRange = wsLeaderboard.Range(1, 1, 1, dataStartCol + 2);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(_primaryColor);
        headerRange.Style.Font.FontColor = XLColor.White;
        
        int row = 2;
        int rank = 1;
        foreach (var score in scores.OrderByDescending(s => s.TotalScore))
        {
            wsLeaderboard.Cell(row, 1).Value = rank++;
            wsLeaderboard.Cell(row, 2).Value = score.UserName;
            wsLeaderboard.Cell(row, 3).Value = score.RoleName;
            
            if (showUnitColumn)
            {
                wsLeaderboard.Cell(row, 4).Value = score.UnitName;
            }

            wsLeaderboard.Cell(row, dataStartCol).Value = score.TasksDone;
            wsLeaderboard.Cell(row, dataStartCol + 1).Value = score.EventsAttended;
            wsLeaderboard.Cell(row, dataStartCol + 2).Value = score.TotalScore;
            row++;
        }
        wsLeaderboard.Columns().AdjustToContents();
    }

    private void GenerateExcelLogSheet(XLWorkbook workbook, UnitActivitySummaryDto summary)
    {
        var wsLog = workbook.Worksheets.Add("Activity Log");
        wsLog.Cell("A1").Value = "Timestamp (UTC)";
        wsLog.Cell("B1").Value = "Action";
        wsLog.Cell("C1").Value = "Entity Type";
        wsLog.Cell("D1").Value = "Unit";
        wsLog.Cell("E1").Value = "Details";
        
        var logHeader = wsLog.Range("A1:E1");
        logHeader.Style.Font.Bold = true;
        logHeader.Style.Fill.BackgroundColor = XLColor.FromHtml(_primaryColor);
        logHeader.Style.Font.FontColor = XLColor.White;

        int logRow = 2;
        foreach (var log in summary.RecentLogs)
        {
            wsLog.Cell(logRow, 1).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm");
            wsLog.Cell(logRow, 2).Value = log.Action;
            wsLog.Cell(logRow, 3).Value = log.EntityType;
            wsLog.Cell(logRow, 4).Value = log.UnitName ?? "";
            wsLog.Cell(logRow, 5).Value = log.Details ?? "";
            logRow++;
        }
        wsLog.Columns().AdjustToContents();
    }

    private void GenerateExcelBurnoutSheet(XLWorkbook workbook, List<BurnoutRiskDto> burnoutRisks)
    {
        var wsBurnout = workbook.Worksheets.Add("Burnout Analysis");
        wsBurnout.Cell("A1").Value = "Member Name";
        wsBurnout.Cell("B1").Value = "Risk Level";
        wsBurnout.Cell("C1").Value = "Primary Risk Factor";
        
        var burnoutHeader = wsBurnout.Range("A1:C1");
        burnoutHeader.Style.Font.Bold = true;
        burnoutHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#ef4444");
        burnoutHeader.Style.Font.FontColor = XLColor.White;

        int bRow = 2;
        foreach (var risk in burnoutRisks.OrderByDescending(r => GetRiskWeight(r.RiskLevel)))
        {
            wsBurnout.Cell(bRow, 1).Value = risk.UserName;
            wsBurnout.Cell(bRow, 2).Value = risk.RiskLevel;
            wsBurnout.Cell(bRow, 3).Value = risk.WarningFlags.FirstOrDefault() ?? "Unknown";
            bRow++;
        }
        wsBurnout.Columns().AdjustToContents();
    }

    private void ComposeHeader(IContainer container, UnitActivitySummaryDto summary)
    {
        container.Row(row =>
        {
            row.ConstantItem(50).Height(50).Svg(LogoSvg);
            row.RelativeItem().PaddingLeft(15).Column(column =>
            {
                column.Item().Text($"OrgTrack Activity Report").FontSize(20).SemiBold().FontColor(_primaryColor);
                column.Item().Text(text =>
                {
                    text.Span("Unit: ").SemiBold();
                    text.Span(summary.UnitName);
                });
                column.Item().Text(text =>
                {
                    text.Span("Generated: ").SemiBold();
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"));
                });
            });
        });
    }

    private void ComposeContent(IContainer container, UnitActivitySummaryDto summary, List<MemberActivityScoreDto> scores, List<BurnoutRiskDto> burnoutRisks, bool showUnitColumn)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);
            ComposeSummaryStats(column, summary);
            ComposeLeaderboard(column, scores, showUnitColumn);
            if (burnoutRisks.Any())
            {
                ComposeBurnoutSection(column, burnoutRisks);
            }
            ComposeRecentActivityLog(column, summary);
        });
    }

    private void ComposeBurnoutSection(ColumnDescriptor column, List<BurnoutRiskDto> burnoutRisks)
    {
        column.Item().Text("Burnout & Well-being Analysis").FontSize(14).SemiBold().FontColor("#ef4444"); // Red-500
        column.Item().PaddingTop(5).Text("The following members have triggered the burnout predictive algorithm. Please review their workload and check in on their well-being.").FontSize(10).FontColor(_mutedColor);
        
        column.Item().PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);  // Name
                columns.RelativeColumn(2);  // Risk Level
                columns.RelativeColumn(4);  // Primary Risk Factor
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(2).BorderColor("#ef4444").PaddingBottom(5).Text("Member").SemiBold();
                header.Cell().BorderBottom(2).BorderColor("#ef4444").PaddingBottom(5).Text("Risk Level").SemiBold();
                header.Cell().BorderBottom(2).BorderColor("#ef4444").PaddingBottom(5).Text("Primary Trigger").SemiBold();
            });

            foreach (var risk in burnoutRisks.OrderByDescending(r => GetRiskWeight(r.RiskLevel)))
            {
                var riskColor = GetRiskColorCode(risk.RiskLevel);
                
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(risk.UserName).FontSize(11).SemiBold();
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(risk.RiskLevel.ToUpper()).FontSize(10).SemiBold().FontColor(riskColor);
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(risk.WarningFlags.FirstOrDefault() ?? "Unknown").FontSize(10).FontColor(_mutedColor);
            }
        });
    }

    private int GetRiskWeight(string level) => level switch
    {
        "Critical" => 3,
        "High" => 2,
        _ => 1
    };

    private string GetRiskColorCode(string level) => level switch
    {
        "Critical" => "#dc2626",
        "High" => "#ea580c",
        _ => "#ca8a04"
    };

    private void ComposeSummaryStats(ColumnDescriptor column, UnitActivitySummaryDto summary)
    {
        column.Item().Background(_surfaceColor).Padding(10).Row(row =>
        {
            row.RelativeItem().AlignCenter().Column(c =>
            {
                c.Item().Text("Tasks Completed").FontColor(_mutedColor).FontSize(10);
                c.Item().Text(summary.TasksDone.ToString()).FontSize(24).SemiBold().FontColor(_primaryColor);
            });
            row.RelativeItem().AlignCenter().Column(c =>
            {
                c.Item().Text("Events Held").FontColor(_mutedColor).FontSize(10);
                c.Item().Text(summary.EventsHeld.ToString()).FontSize(24).SemiBold().FontColor(_primaryColor);
            });
            row.RelativeItem().AlignCenter().Column(c =>
            {
                c.Item().Text("Active Members").FontColor(_mutedColor).FontSize(10);
                c.Item().Text(summary.MembersActive.ToString()).FontSize(24).SemiBold().FontColor(_primaryColor);
            });
        });
    }

    private void ComposeLeaderboard(ColumnDescriptor column, List<MemberActivityScoreDto> scores, bool showUnitColumn)
    {
        column.Item().Text("Member Leaderboard").FontSize(14).SemiBold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30); // Rank
                columns.RelativeColumn(3);  // Name
                columns.RelativeColumn(3);  // Role
                if (showUnitColumn)
                {
                    columns.RelativeColumn(3);  // Unit
                }
                columns.ConstantColumn(50); // Tasks
                columns.ConstantColumn(50); // Events
                columns.ConstantColumn(50); // Score
            });

            table.Header(header =>
            {
                header.Cell().Background(_primaryColor).Padding(5).Text("#").FontColor(Colors.White).SemiBold();
                header.Cell().Background(_primaryColor).Padding(5).Text("Name").FontColor(Colors.White).SemiBold();
                header.Cell().Background(_primaryColor).Padding(5).Text("Role").FontColor(Colors.White).SemiBold();
                if (showUnitColumn)
                {
                    header.Cell().Background(_primaryColor).Padding(5).Text("Unit").FontColor(Colors.White).SemiBold();
                }
                header.Cell().Background(_primaryColor).Padding(5).AlignRight().Text("Tasks").FontColor(Colors.White).SemiBold();
                header.Cell().Background(_primaryColor).Padding(5).AlignRight().Text("Events").FontColor(Colors.White).SemiBold();
                header.Cell().Background(_primaryColor).Padding(5).AlignRight().Text("Score").FontColor(Colors.White).SemiBold();
            });

            int rank = 1;
            foreach (var score in scores.OrderByDescending(s => s.TotalScore))
            {
                string bg = rank % 2 == 0 ? Colors.White : _surfaceColor;
                
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).Text(rank.ToString());
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).Text(score.UserName);
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).Text(score.RoleName);
                if (showUnitColumn)
                {
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).Text(score.UnitName);
                }
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).AlignRight().Text(score.TasksDone.ToString());
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).AlignRight().Text(score.EventsAttended.ToString());
                table.Cell().Background(bg).BorderBottom(1).BorderColor(_borderColor).Padding(5).AlignRight().Text(score.TotalScore.ToString()).SemiBold();
                
                rank++;
            }
        });
    }

    private void ComposeRecentActivityLog(ColumnDescriptor column, UnitActivitySummaryDto summary)
    {
        column.Item().Text("Recent Activity").FontSize(14).SemiBold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(120); // Date
                columns.ConstantColumn(100); // Action
                columns.RelativeColumn();    // Unit
                columns.RelativeColumn();    // Details
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(2).BorderColor(_primaryColor).PaddingBottom(5).Text("Date").SemiBold();
                header.Cell().BorderBottom(2).BorderColor(_primaryColor).PaddingBottom(5).Text("Action").SemiBold();
                header.Cell().BorderBottom(2).BorderColor(_primaryColor).PaddingBottom(5).Text("Unit").SemiBold();
                header.Cell().BorderBottom(2).BorderColor(_primaryColor).PaddingBottom(5).Text("Details").SemiBold();
            });

            foreach (var log in summary.RecentLogs)
            {
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(10);
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(log.Action).FontSize(10);
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(log.UnitName ?? "").FontSize(10);
                table.Cell().PaddingVertical(5).BorderBottom(1).BorderColor(_borderColor).Text(log.Details ?? "").FontSize(10);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }
}

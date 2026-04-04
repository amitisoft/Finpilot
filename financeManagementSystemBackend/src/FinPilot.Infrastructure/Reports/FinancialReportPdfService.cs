using FinPilot.Application.DTOs.Agents;
using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.DTOs.Forecast;
using FinPilot.Application.DTOs.Insights;
using FinPilot.Application.DTOs.Reports;
using FinPilot.Application.Interfaces.Forecast;
using FinPilot.Application.Interfaces.Insights;
using FinPilot.Application.Interfaces.Reports;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinPilot.Infrastructure.Reports;

public sealed class FinancialReportPdfService(
    FinPilotDbContext dbContext,
    IReportsService reportsService,
    IForecastService forecastService,
    IHealthScoreService healthScoreService,
    ReportGeneratorAgentService reportGeneratorAgentService,
    InsightContextBuilder contextBuilder) : IReportExportService
{
    static FinancialReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(byte[] Content, string FileName)> GenerateFinancialReportPdfAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.FullName, x.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var context = await contextBuilder.BuildAsync(userId, 6, cancellationToken);
        var forecast = await forecastService.GetMonthlyForecastAsync(userId, cancellationToken);
        var trends = await reportsService.GetTrendsAsync(userId, 6, cancellationToken);
        var netWorth = await reportsService.GetNetWorthAsync(userId, 6, cancellationToken);
        var health = await healthScoreService.GetAsync(userId, cancellationToken);
        var report = reportGeneratorAgentService.Analyze(context);

        var topCategories = context.CategoryBreakdown.Take(5).ToList();
        var budgets = context.BudgetHealth.Take(4).ToList();
        var goals = context.Goals.Take(4).ToList();
        var generatedAt = DateTimeOffset.UtcNow;
        var fileName = $"finpilot-financial-report-{generatedAt:yyyy-MM-dd}.pdf";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor("#0F172A"));

                page.Header().Element(header => ComposeHeader(header, user?.FullName ?? "FinPilot User", user?.Email ?? string.Empty, generatedAt));
                page.Content().Element(content => ComposeContent(content, context, forecast, trends, netWorth, health, report, topCategories, budgets, goals));
                page.Footer().Element(ComposeFooter);
            });
        });

        return (document.GeneratePdf(), fileName);
    }

    private static void ComposeHeader(IContainer container, string fullName, string email, DateTimeOffset generatedAt)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Spacing(4);
                    left.Item().Text("FINPILOT").FontSize(11).SemiBold().FontColor("#4F46E5");
                    left.Item().Text("Personal Financial Report").FontSize(24).Bold().FontColor("#0F172A");
                    left.Item().Text("A concise executive report covering performance, forecast, balance trajectory, and the next actions that matter most.")
                        .FontSize(10).FontColor("#475569");
                });

                row.ConstantItem(190).AlignRight().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(12).Column(right =>
                {
                    right.Spacing(4);
                    right.Item().Text("Prepared for").FontSize(9).SemiBold().FontColor("#64748B");
                    right.Item().Text(fullName).FontSize(12).Bold();
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        right.Item().Text(email).FontSize(9).FontColor("#64748B");
                    }

                    right.Item().PaddingTop(6).Text($"Generated {generatedAt:dd MMM yyyy, hh:mm tt} UTC").FontSize(9).FontColor("#64748B");
                });
            });

            column.Item().LineHorizontal(1).LineColor("#E2E8F0");
        });
    }

    private static void ComposeContent(
        IContainer container,
        InsightContext context,
        MonthlyForecastResponse forecast,
        IReadOnlyCollection<ReportTrendPointResponse> trends,
        IReadOnlyCollection<NetWorthPointResponse> netWorth,
        HealthScoreResponse health,
        ReportGeneratorAnalysisResponse report,
        IReadOnlyCollection<CategoryBreakdownResponse> topCategories,
        IReadOnlyCollection<BudgetStatusResponse> budgets,
        IReadOnlyCollection<InsightGoalContext> goals)
    {
        container.PaddingTop(18).Column(column =>
        {
            column.Spacing(18);

            column.Item().Element(c => ComposeExecutiveSummary(c, context, forecast, health));

            column.Item().Row(row =>
            {
                row.Spacing(12);
                row.RelativeItem().Element(c => ComposeMetricCard(c, "Income", FormatMoney(context.Summary.TotalIncome), "Tracked inflows this month", "#ECFDF5", "#047857"));
                row.RelativeItem().Element(c => ComposeMetricCard(c, "Expenses", FormatMoney(context.Summary.TotalExpenses), "Tracked outflows this month", "#FEF2F2", "#BE123C"));
                row.RelativeItem().Element(c => ComposeMetricCard(c, "Projected balance", FormatMoney(forecast.ProjectedEndOfMonthBalance), $"{forecast.DaysRemaining} day(s) remaining", "#EEF2FF", "#4338CA"));
                row.RelativeItem().Element(c => ComposeMetricCard(c, "Health score", $"{health.Score}/100", health.Label, "#F8FAFC", "#0F172A"));
            });

            column.Item().Element(c => ComposeSectionCard(c, report.Title, report.Summary, report.Forecast, report.Highlights));
            column.Item().Element(c => ComposeTrendTable(c, trends));
            column.Item().Element(c => ComposeNetWorthTable(c, netWorth));
            column.Item().Element(c => ComposeCategorySection(c, topCategories));
            column.Item().Element(c => ComposeBudgetGoalSection(c, budgets, goals));
            column.Item().Element(c => ComposeHealthSection(c, health));
            column.Item().Element(c => ComposeRecommendationSection(c, report.Highlights, health.Suggestions));
        });
    }

    private static void ComposeExecutiveSummary(IContainer container, InsightContext context, MonthlyForecastResponse forecast, HealthScoreResponse health)
    {
        container.Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(18).Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("Executive summary").FontSize(14).Bold();
            column.Item().Text(text =>
            {
                text.Span("Current balance stands at ").FontColor("#475569");
                text.Span(FormatMoney(context.Summary.TotalBalance)).SemiBold();
                text.Span(", with net monthly cashflow at ").FontColor("#475569");
                text.Span(FormatMoney(context.Summary.NetAmount)).SemiBold();
                text.Span(" and a projected end-of-month balance of ").FontColor("#475569");
                text.Span(FormatMoney(forecast.ProjectedEndOfMonthBalance)).SemiBold();
                text.Span($". Overall financial health is assessed at {health.Score}/100 ({health.Label}).").FontColor("#475569");
            });
        });
    }

    private static void ComposeMetricCard(IContainer container, string label, string value, string helper, string background, string accent)
    {
        container.Border(1).BorderColor("#E2E8F0").Background(background).Padding(14).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text(label.ToUpperInvariant()).FontSize(8).SemiBold().FontColor("#64748B");
            column.Item().Text(value).FontSize(17).Bold().FontColor(accent);
            column.Item().Text(helper).FontSize(9).FontColor("#64748B");
        });
    }

    private static void ComposeSectionCard(IContainer container, string title, string summary, string forecast, IReadOnlyCollection<string> highlights)
    {
        container.Border(1).BorderColor("#E2E8F0").Padding(18).Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(title).FontSize(14).Bold();
            column.Item().Text(summary).FontColor("#334155");
            column.Item().Text(forecast).FontColor("#334155");
            if (highlights.Count > 0)
            {
                column.Item().PaddingTop(4).Text("Key highlights").SemiBold();
                foreach (var highlight in highlights.Take(5))
                {
                    column.Item().Text($"• {highlight}").FontColor("#475569");
                }
            }
        });
    }

    private static void ComposeTrendTable(IContainer container, IReadOnlyCollection<ReportTrendPointResponse> trends)
    {
        ComposeTableSection(
            container,
            "Cashflow trend (6 months)",
            ["Period", "Income", "Expense", "Net"],
            trends.Select(point => new[]
            {
                point.Label,
                FormatMoney(point.Income),
                FormatMoney(point.Expense),
                FormatMoney(point.NetAmount)
            }));
    }

    private static void ComposeNetWorthTable(IContainer container, IReadOnlyCollection<NetWorthPointResponse> netWorth)
    {
        ComposeTableSection(
            container,
            "Net worth trajectory",
            ["Period", "Net worth"],
            netWorth.Select(point => new[]
            {
                point.Label,
                FormatMoney(point.NetWorth)
            }));
    }

    private static void ComposeCategorySection(IContainer container, IReadOnlyCollection<CategoryBreakdownResponse> categories)
    {
        container.Border(1).BorderColor("#E2E8F0").Padding(18).Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Top expense categories").FontSize(14).Bold();

            if (categories.Count == 0)
            {
                column.Item().Text("Not enough category data has been recorded yet to establish a spending mix.").FontColor("#64748B");
                return;
            }

            foreach (var category in categories)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(category.CategoryName).SemiBold();
                    row.ConstantItem(90).AlignRight().Text($"{category.Percentage:0.##}%").FontColor("#64748B");
                    row.ConstantItem(120).AlignRight().Text(FormatMoney(category.Amount)).SemiBold();
                });
            }
        });
    }

    private static void ComposeBudgetGoalSection(IContainer container, IReadOnlyCollection<BudgetStatusResponse> budgets, IReadOnlyCollection<InsightGoalContext> goals)
    {
        container.Column(column =>
        {
            column.Spacing(12);
            column.Item().Element(c => ComposeTableSection(
                c,
                "Budget status",
                ["Budget", "Spent", "Remaining", "Usage"],
                budgets.Count == 0
                    ? [new[] { "No active budget", "—", "—", "—" }]
                    : budgets.Select(item => new[]
                    {
                        item.BudgetName,
                        FormatMoney(item.TotalSpent),
                        FormatMoney(item.RemainingAmount),
                        $"{item.UsagePercent:0.##}%"
                    })));

            column.Item().Element(c => ComposeTableSection(
                c,
                "Goal progress",
                ["Goal", "Progress", "Current", "Target"],
                goals.Count == 0
                    ? [new[] { "No active goal", "—", "—", "—" }]
                    : goals.Select(goal => new[]
                    {
                        goal.GoalName,
                        $"{goal.ProgressPercent:0.##}%",
                        FormatMoney(goal.CurrentAmount),
                        FormatMoney(goal.TargetAmount)
                    })));
        });
    }

    private static void ComposeHealthSection(IContainer container, HealthScoreResponse health)
    {
        container.Border(1).BorderColor("#E2E8F0").Padding(18).Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Financial health breakdown").FontSize(14).Bold();

            foreach (var item in health.Breakdown)
            {
                column.Item().BorderBottom(1).BorderColor("#E2E8F0").PaddingBottom(8).Column(entry =>
                {
                    entry.Spacing(3);
                    entry.Item().Row(row =>
                    {
                        row.RelativeItem().Text(item.Category).SemiBold();
                        row.ConstantItem(80).AlignRight().Text(item.Status.ToUpperInvariant()).FontSize(8).SemiBold().FontColor("#64748B");
                    });
                    entry.Item().Text(item.Summary).FontColor("#475569");
                });
            }
        });
    }

    private static void ComposeRecommendationSection(IContainer container, IReadOnlyCollection<string> highlights, IReadOnlyCollection<string> suggestions)
    {
        container.Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(18).Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("Recommended next actions").FontSize(14).Bold();

            var rendered = false;
            foreach (var suggestion in suggestions.Take(4))
            {
                rendered = true;
                column.Item().Text($"• {suggestion}").FontColor("#334155");
            }

            if (highlights.Count > 0)
            {
                column.Item().PaddingTop(4).Text("Analyst notes").SemiBold();
                foreach (var highlight in highlights.Take(3))
                {
                    rendered = true;
                    column.Item().Text($"• {highlight}").FontColor("#475569");
                }
            }

            if (!rendered)
            {
                column.Item().Text("• Keep tracking income, expenses, budgets, and goals consistently to unlock stronger recommendations.").FontColor("#475569");
            }
        });
    }

    private static void ComposeTableSection(IContainer container, string title, IReadOnlyCollection<string> headers, IEnumerable<string[]> rows)
    {
        container.Border(1).BorderColor("#E2E8F0").Padding(18).Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(title).FontSize(14).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var columnTitle in headers)
                    {
                        header.Cell().Element(HeaderCell).Text(columnTitle).SemiBold().FontColor("#475569");
                    }
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                    {
                        table.Cell().Element(BodyCell).Text(cell).FontColor("#0F172A");
                    }
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer container)
        => container.Background("#F8FAFC").BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).PaddingHorizontal(6);

    private static IContainer BodyCell(IContainer container)
        => container.BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(8).PaddingHorizontal(6);

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(10).Row(row =>
        {
            row.RelativeItem().Text("FinPilot report for informational use only. Review source transactions before taking financial action.")
                .FontSize(8)
                .FontColor("#64748B");
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor("#64748B");
                text.CurrentPageNumber().FontSize(8).SemiBold();
            });
        });
    }

    private static string FormatMoney(decimal amount) => $"₹{amount:N2}";
}

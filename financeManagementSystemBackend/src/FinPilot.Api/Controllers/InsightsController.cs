using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Insights;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class InsightsController(
    IInsightsService insightsService,
    IHealthScoreService healthScoreService,
    ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<InsightsOverviewResponse>>> GetOverview(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var monthlyTask = insightsService.GetMonthlyInsightsAsync(userId, 6, cancellationToken);
        var budgetTask = insightsService.GetBudgetRiskInsightsAsync(userId, cancellationToken);
        var anomalyTask = insightsService.GetAnomalyInsightsAsync(userId, cancellationToken);
        var goalTask = insightsService.GetGoalInsightsAsync(userId, cancellationToken);
        var healthTask = healthScoreService.GetAsync(userId, cancellationToken);

        await Task.WhenAll(monthlyTask, budgetTask, anomalyTask, goalTask, healthTask);

        var monthly = await monthlyTask;
        var budget = await budgetTask;
        var anomaly = await anomalyTask;
        var goal = await goalTask;
        var health = await healthTask;

        var response = new InsightsOverviewResponse
        {
            Headline = $"{health.Label} health score at {health.Score}/100. {monthly.Headline}",
            HealthScore = health.Score,
            HealthLabel = health.Label,
            Sections =
            [
                ToSection("monthly", "Monthly", monthly),
                ToSection("budget", "Budget risk", budget),
                ToSection("anomalies", "Anomalies", anomaly),
                ToSection("goals", "Goals", goal)
            ],
            GeneratedAt = new[] { monthly.GeneratedAt, budget.GeneratedAt, anomaly.GeneratedAt, goal.GeneratedAt, health.GeneratedAt }.Max()
        };

        return Success(response, "Insights overview generated successfully");
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<ApiResponse<InsightBundleResponse>>> GetMonthly([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var userId = EnsureUser();
        var item = await insightsService.GetMonthlyInsightsAsync(userId, months, cancellationToken);
        return Success(item, "Monthly insights generated successfully");
    }

    [HttpGet("budget-risk")]
    public async Task<ActionResult<ApiResponse<InsightBundleResponse>>> GetBudgetRisk(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await insightsService.GetBudgetRiskInsightsAsync(userId, cancellationToken);
        return Success(item, "Budget risk insights generated successfully");
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<ApiResponse<InsightBundleResponse>>> GetAnomalies(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await insightsService.GetAnomalyInsightsAsync(userId, cancellationToken);
        return Success(item, "Anomaly insights generated successfully");
    }

    [HttpGet("goals")]
    public async Task<ActionResult<ApiResponse<InsightBundleResponse>>> GetGoals(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await insightsService.GetGoalInsightsAsync(userId, cancellationToken);
        return Success(item, "Goal insights generated successfully");
    }

    [HttpGet("health-score")]
    public async Task<ActionResult<ApiResponse<HealthScoreResponse>>> GetHealthScore(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await healthScoreService.GetAsync(userId, cancellationToken);
        return Success(item, "Health score generated successfully");
    }

    private static InsightsOverviewSectionResponse ToSection(string key, string title, InsightBundleResponse bundle)
    {
        return new InsightsOverviewSectionResponse
        {
            Key = key,
            Title = title,
            Headline = bundle.Headline,
            Priority = GetPriority(bundle)
        };
    }

    private static string GetPriority(InsightBundleResponse bundle)
    {
        if (bundle.Cards.Any(x => string.Equals(x.Priority, "high", StringComparison.OrdinalIgnoreCase)))
        {
            return "high";
        }

        if (bundle.Cards.Any(x => string.Equals(x.Priority, "medium", StringComparison.OrdinalIgnoreCase)))
        {
            return "medium";
        }

        return "low";
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
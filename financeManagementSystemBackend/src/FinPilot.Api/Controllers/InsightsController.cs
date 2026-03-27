using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Insights;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class InsightsController(IInsightsService insightsService, ICurrentUserService currentUserService) : BaseApiController
{
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

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}

using FinPilot.Application.DTOs.Insights;

namespace FinPilot.Application.Interfaces.Insights;

public interface IInsightsService
{
    Task<InsightBundleResponse> GetMonthlyInsightsAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default);
    Task<InsightBundleResponse> GetBudgetRiskInsightsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<InsightBundleResponse> GetAnomalyInsightsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<InsightBundleResponse> GetGoalInsightsAsync(Guid userId, CancellationToken cancellationToken = default);
}

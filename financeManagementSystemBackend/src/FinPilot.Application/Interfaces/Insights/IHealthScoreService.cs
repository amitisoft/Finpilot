using FinPilot.Application.DTOs.Insights;

namespace FinPilot.Application.Interfaces.Insights;

public interface IHealthScoreService
{
    Task<HealthScoreResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}